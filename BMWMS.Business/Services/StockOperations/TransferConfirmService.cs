using System.Data;
using Microsoft.EntityFrameworkCore;
using BMWMS.Business.DTOs.StockOperations;
using BMWMS.Business.Interfaces.StockOperations;
using BMWMS.Business.Interfaces;
using BMWMS.Business.DTOs.Capacity;
using BMWMS.Business.DTOs.Audit;
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
            // === LAYER 1: DB Transaction Serializable ===
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var order = await _repository.GetOrderWithDetailsAsync(transferOrderId);
                if (order == null) throw new ArgumentException("Phiếu không tồn tại.");

                var allocations = new List<CapacityAllocationDto>();
                var sourceKeys = new List<(long ProductId, long LotId, long LocationId, decimal Qty)>();

                foreach (var inputItem in dto.Items)
                {
                    var detail = order.TransferOrderDetails.FirstOrDefault(d => d.TransferOrderDetailId == inputItem.TransferOrderDetailId);
                    if (detail == null) continue;

                    var destId = inputItem.DestinationLocationId ?? detail.DestinationLocationId ?? 0;
                    var confirmQuantity = detail.RequestedQuantity;
                    if (confirmQuantity > 0)
                    {
                        allocations.Add(new CapacityAllocationDto
                        {
                            StorageLocationId = destId,
                            ProductId = detail.ProductId,
                            Quantity = confirmQuantity
                        });
                        
                        sourceKeys.Add((detail.ProductId, detail.ProductLotId ?? 0, detail.SourceLocationId ?? 0, confirmQuantity));
                    }
                }

                // === LAYER 2: UPDLOCK trên Inventory source ===
                // Sort to avoid deadlocks: ProductId -> LotId -> LocationId
                var sortedSources = sourceKeys.OrderBy(k => k.ProductId).ThenBy(k => k.LotId).ThenBy(k => k.LocationId).ToList();
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

                    // Chú ý: Vì lượng hàng này đã được cộng vào ReservedQuantity lúc Approve, 
                    // ta cần check xem tổng tồn kho OnHand có còn đủ không (khi Confirm ta sẽ xả Reserve và trừ OnHand).
                    if (inv.OnHandQuantity < src.Qty)
                        throw new InvalidOperationException($"Tồn kho thực tế tại nguồn không đủ để xuất: yêu cầu {src.Qty}, thực tế {inv.OnHandQuantity}.");
                }

                // === LAYER 3: UPDLOCK/HOLDLOCK trên Zone -> Rack -> Location đích ===
                if (allocations.Any())
                {
                    var evaluations = await _capacityService.EvaluateAsync(allocations, acquireLocationLocks: true);
                    var exceededLocs = evaluations.Values
                        .Where(e => e.OverallStatus == CapacityEvaluationStatuses.Exceeded)
                        .Select(e => e.LocationCode)
                        .ToList();

                    if (exceededLocs.Any())
                    {
                        if (!dto.AcknowledgeCapacityWarning)
                        {
                            return new TransferResultDto
                            {
                                Success = false,
                                Message = $"Vị trí đích đã bị đầy bởi một giao dịch khác: {string.Join(", ", exceededLocs)}.\nVui lòng đổi vị trí đích và điền lý do."
                            };
                        }
                    }
                }

                var repoParams = dto.Items.Select(i =>
                {
                    var detail = order.TransferOrderDetails.First(d => d.TransferOrderDetailId == i.TransferOrderDetailId);
                    return new TransferConfirmItemParam
                    {
                        TransferOrderDetailId = i.TransferOrderDetailId,
                        ActualMovedQuantity = detail.RequestedQuantity,
                        DestinationLocationId = i.DestinationLocationId
                    };
                }).ToList();

                var confirmedOrder = await _repository.ConfirmTransferAsync(
                    transferOrderId, staffId, repoParams, dto.DestinationChangeReason, dto.ShortfallReason, dto.Notes);

                await _auditLogService.RecordAsync(new AuditEventDto { UserId = staffId, ActionType = "CONFIRM_TRANSFER", EntityName = "TransferOrder", EntityId = transferOrderId.ToString() });

                await tx.CommitAsync();

                return new TransferResultDto { Success = true, Message = "Xác nhận chuyển kho thành công." };
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}

