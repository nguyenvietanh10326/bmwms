using System.Data;
using Microsoft.EntityFrameworkCore;
using BMWMS.Business.DTOs.StockOperations;
using BMWMS.Business.Interfaces.StockOperations;
using BMWMS.Business.Interfaces;
using BMWMS.Business.DTOs.Capacity;
using BMWMS.Business.DTOs.Audit;
using BMWMS.Business.Common;
using BMWMS.Repository.Interfaces.StockOperations;
using BMWMS.Repository.Models;

namespace BMWMS.Business.Services.StockOperations
{
    public class TransferConfirmService : ITransferConfirmService
    {
        private readonly ITransferRepository _repository;
        private readonly ICapacityEvaluationService _capacityService;
        private readonly IAuditLogService _auditLogService;
        private readonly BmwmsContext _context;

        public TransferConfirmService(
            ITransferRepository repository, 
            ICapacityEvaluationService capacityService,
            IAuditLogService auditLogService,
            BmwmsContext context)
        {
            _repository = repository;
            _capacityService = capacityService;
            _auditLogService = auditLogService;
            _context = context;
        }

        public async Task<TransferResultDto> ConfirmAsync(long staffId, long transferOrderId, ConfirmTransferDto dto)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                await OrderWorkflowLock.AcquireAsync(_context, "TRANSFER", transferOrderId);
                var order = await _repository.GetOrderWithDetailsAsync(transferOrderId);
                if (order == null) throw new ArgumentException("Phiếu không tồn tại.");
                if (order.Status != "DRAFT")
                    throw new InvalidOperationException("Chỉ thực hiện phiếu chưa hoàn tất.");
                if (order.AssignedToUserId != staffId)
                    throw new UnauthorizedAccessException("Chỉ nhân viên kho được giao mới được xác nhận chuyển kho.");
                dto ??= new ConfirmTransferDto();

                var allocations = new List<CapacityAllocationDto>();
                var sourceKeys = new List<(long ProductId, long LotId, long LocationId, decimal Qty)>();
                var confirmedItems = new List<TransferConfirmItemParam>();
                var requestedItems = dto.Items ?? new List<ConfirmTransferItemDto>();
                if (requestedItems.GroupBy(item => item.TransferOrderDetailId).Any(group => group.Count() > 1) ||
                    requestedItems.Any(item => order.TransferOrderDetails.All(detail => detail.TransferOrderDetailId != item.TransferOrderDetailId)))
                    throw new ArgumentException("Dữ liệu thực hiện không khớp các dòng phiếu chuyển kho.");

                foreach (var detail in order.TransferOrderDetails)
                {
                    var requestedItem = requestedItems.FirstOrDefault(item => item.TransferOrderDetailId == detail.TransferOrderDetailId);
                    var destId = requestedItem?.DestinationLocationId ?? detail.DestinationLocationId ?? 0;
                    var confirmQuantity = requestedItem?.ActualMovedQuantity ?? detail.RequestedQuantity;
                    if (confirmQuantity < 0 || confirmQuantity > detail.RequestedQuantity)
                        throw new ArgumentException($"Số thực chuyển phải từ 0 đến {detail.RequestedQuantity}.");
                    if (confirmQuantity > 0 && detail.Product != null)
                        QuantityRules.EnsureValid(detail.Product, confirmQuantity, "Số thực chuyển");
                    if (destId <= 0 || destId == detail.SourceLocationId)
                        throw new ArgumentException("Vị trí đích phải khác vị trí nguồn và còn tồn tại.");
                    confirmedItems.Add(new TransferConfirmItemParam {
                        TransferOrderDetailId = detail.TransferOrderDetailId,
                        ActualMovedQuantity = confirmQuantity,
                        DestinationLocationId = destId
                    });
                    if (confirmQuantity > 0)
                    {
                        allocations.Add(new CapacityAllocationDto { StorageLocationId = destId, ProductId = detail.ProductId, Quantity = confirmQuantity });
                        // The source is freed by the same physical movement. Evaluate the net Bin/Rack/Zone change.
                        allocations.Add(new CapacityAllocationDto { StorageLocationId = detail.SourceLocationId!.Value, ProductId = detail.ProductId, Quantity = -confirmQuantity });
                        sourceKeys.Add((detail.ProductId, detail.ProductLotId ?? 0, detail.SourceLocationId ?? 0, confirmQuantity));
                    }
                }
                if (sourceKeys.Count == 0)
                    throw new ArgumentException("Không có hàng thực chuyển; hãy hủy phiếu thay vì xác nhận rỗng.");
                if (confirmedItems.Any(item => item.ActualMovedQuantity <
                        order.TransferOrderDetails.Single(detail => detail.TransferOrderDetailId == item.TransferOrderDetailId).RequestedQuantity) &&
                    (dto.ShortfallReason?.Trim().Length ?? 0) < 5)
                    throw new ArgumentException("Chuyển thiếu so với phiếu phải có lý do ít nhất 5 ký tự.");

                var destinationIds = confirmedItems.Where(item => item.ActualMovedQuantity > 0)
                    .Select(item => item.DestinationLocationId!.Value).Distinct().ToArray();
                var destinations = await _context.StorageLocations.AsNoTracking()
                    .Where(location => destinationIds.Contains(location.StorageLocationId))
                    .ToDictionaryAsync(location => location.StorageLocationId);
                foreach (var destinationId in destinationIds)
                {
                    if (!destinations.TryGetValue(destinationId, out var destination) ||
                        destination.WarehouseId != order.DestinationWarehouseId || destination.RackId == null ||
                        !destination.IsPutawayAllowed || destination.Status is "BLOCKED" or "INACTIVE")
                        throw new InvalidOperationException("Vị trí đích đã đổi, bị khóa hoặc không thuộc kho hiện tại. Vui lòng chọn lại Bin.");
                }

                var sortedSources = sourceKeys.GroupBy(key => new { key.ProductId, key.LotId, key.LocationId })
                    .Select(group => (group.Key.ProductId, group.Key.LotId, group.Key.LocationId, Qty: group.Sum(item => item.Qty)))
                    .OrderBy(key => key.ProductId).ThenBy(key => key.LotId).ThenBy(key => key.LocationId).ToList();
                foreach (var src in sortedSources)
                {
                    // UPDLOCK and HOLDLOCK to protect this row against other concurrent reads/writes
                    var inv = await _context.Inventories
                        .FromSqlInterpolated($@"
                            SELECT * FROM dbo.Inventory WITH (UPDLOCK, HOLDLOCK) 
                            WHERE ProductID = {src.ProductId} 
                              AND ProductLotID = {src.LotId} 
                              AND StorageLocationID = {src.LocationId}")
                        .FirstOrDefaultAsync();

                    if (inv == null)
                        throw new InvalidOperationException($"Không tìm thấy tồn kho nguồn cho sản phẩm {src.ProductId} tại vị trí {src.LocationId}.");

                    var available = inv.OnHandQuantity - inv.ReservedQuantity;
                    if (available < src.Qty)
                        throw new InvalidOperationException(
                            $"Tồn khả dụng tại nguồn không đủ để chuyển: yêu cầu {src.Qty}, khả dụng {available}. " +
                            "Phần đang giữ cho đơn bán hàng hoặc phiếu xuất không được dùng cho chuyển kho.");
                }

                if (allocations.Any())
                {
                    var evaluations = await _capacityService.EvaluateAsync(allocations, acquireLocationLocks: true);
                    var exceededLocs = evaluations.Values.Where(e => destinationIds.Contains(e.StorageLocationId))
                        .Where(e => e.OverallStatus == CapacityEvaluationStatuses.Exceeded)
                        .Select(e => e.LocationCode)
                        .ToList();

                    if (exceededLocs.Any())
                        throw new InvalidOperationException($"Vị trí đích vượt sức chứa: {string.Join(", ", exceededLocs)}. Không thể xác nhận dù đã đánh dấu cảnh báo.");
                    var unknown = evaluations.Values.Where(e => destinationIds.Contains(e.StorageLocationId))
                        .Where(e => e.OverallStatus is CapacityEvaluationStatuses.Unknown or CapacityEvaluationStatuses.NotConfigured)
                        .Select(e => e.LocationCode).ToList();
                    if (unknown.Any() && (!dto.AcknowledgeCapacityWarning || (dto.CapacityWarningReason?.Trim().Length ?? 0) < 5))
                        throw new InvalidOperationException($"Chưa xác định sức chứa tại {string.Join(", ", unknown)}; cần xác nhận và ghi lý do ít nhất 5 ký tự.");
                }

                await _repository.ConfirmTransferAsync(
                    transferOrderId, staffId, confirmedItems, dto.DestinationChangeReason, dto.ShortfallReason, dto.Notes);

                await _auditLogService.RecordAsync(new AuditEventDto { UserId = staffId, ActionType = "EXECUTE_TRANSFER", EntityName = "TransferOrder", EntityId = transferOrderId.ToString() });

                await tx.CommitAsync();

                return new TransferResultDto { Success = true, Message = "Thực hiện chuyển kho thành công." };
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}

