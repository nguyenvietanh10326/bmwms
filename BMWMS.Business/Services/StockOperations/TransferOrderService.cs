using BMWMS.Business.DTOs.StockOperations;
using BMWMS.Business.Interfaces.StockOperations;
using BMWMS.Business.Interfaces;
using BMWMS.Business.DTOs.Capacity;
using BMWMS.Business.DTOs.Audit;
using BMWMS.Repository.Interfaces.StockOperations;

namespace BMWMS.Business.Services.StockOperations
{
    public class TransferOrderService : ITransferOrderService
    {
        private readonly ITransferRepository _repository;
        private readonly ICapacityEvaluationService _capacityService;
        private readonly IAuditLogService _auditLogService;

        public TransferOrderService(
            ITransferRepository repository, 
            ICapacityEvaluationService capacityService, 
            IAuditLogService auditLogService)
        {
            _repository = repository;
            _capacityService = capacityService;
            _auditLogService = auditLogService;
        }

        public async Task<TransferOrderPagedResultDto> GetPagedOrdersAsync(string? keyword, string? status, long? warehouseId, int pageIndex, int pageSize, long? currentStaffId)
        {
            var (items, total, draft, approved, completed, cancelled) = await _repository.GetPagedOrdersAsync(keyword, status, warehouseId, pageIndex, pageSize, currentStaffId);

            var list = items.Select(o => new TransferOrderListDto
            {
                TransferOrderId = o.TransferOrderId,
                TransferOrderNumber = o.TransferOrderNumber,
                TransferType = o.TransferType,
                WarehouseName = o.SourceWarehouse?.WarehouseName ?? "N/A",
                SourceLocationSummary = string.Join(", ", o.TransferOrderDetails.Select(d => d.SourceLocationId).Distinct()), // Simplified
                DestinationLocationSummary = string.Join(", ", o.TransferOrderDetails.Select(d => d.DestinationLocationId).Distinct()),
                Status = o.Status,
                StatusLabel = o.Status switch { "DRAFT" => "Nháp", "APPROVED" => "Đã duyệt", "ASSIGNED" => "Phiếu cũ - chỉ xem", "COMPLETED" => "Hoàn thành", "CANCELLED" => "Đã hủy", _ => o.Status },
                StatusCss = o.Status switch { "DRAFT" => "secondary", "APPROVED" => "primary", "COMPLETED" => "success", "CANCELLED" => "danger", _ => "secondary" },
                CreatedByName = o.CreatedByUser?.FullName ?? o.CreatedByUser?.Username ?? "",
                ApprovedByName = o.ApprovedByUser?.FullName,
                AssignedToName = o.AssignedToUser?.FullName,
                ConfirmedByName = o.ConfirmedByUser?.FullName,
                RequestedDate = o.RequestedDate,
                DueDate = o.DueDate,
                CreatedAt = o.CreatedAt,
                ApprovedAt = o.ApprovedAt,
                ConfirmedAt = o.ConfirmedAt,
                TotalItems = o.TransferOrderDetails.Count,
                TotalRequestedQuantity = o.TransferOrderDetails.Sum(d => d.RequestedQuantity),
                TotalMovedQuantity = o.TransferOrderDetails.Sum(d => d.MovedQuantity)
            }).ToList();

            return new TransferOrderPagedResultDto
            {
                Items = list, TotalCount = total, PageIndex = pageIndex, PageSize = pageSize,
                DraftCount = draft, ApprovedCount = approved, CompletedCount = completed, CancelledCount = cancelled
            };
        }

        public async Task<TransferOrderDetailViewDto?> GetOrderDetailAsync(long transferOrderId, long currentUserId, bool isManager)
        {
            var order = await _repository.GetOrderWithDetailsAsync(transferOrderId);
            if (order == null) return null;
            if (!isManager && order.CreatedByUserId != currentUserId && order.AssignedToUserId != currentUserId) return null;

            var inventoryPosted = order.TransferOrderDetails.Any(d => d.InventoryTransactions.Any());

            return new TransferOrderDetailViewDto
            {
                TransferOrderId = order.TransferOrderId,
                TransferOrderNumber = order.TransferOrderNumber,
                TransferType = order.TransferType,
                Status = order.Status,
                StatusLabel = order.Status switch { "DRAFT" => "Nháp", "APPROVED" => "Đã duyệt", "ASSIGNED" => "Phiếu cũ - chỉ xem", "COMPLETED" => "Hoàn thành", "CANCELLED" => "Đã hủy", _ => order.Status },
                WarehouseName = order.SourceWarehouse?.WarehouseName ?? "",
                RequestedDate = order.RequestedDate,
                DueDate = order.DueDate,
                Notes = order.Notes,
                CreatedByName = order.CreatedByUser?.FullName ?? "",
                CreatedAt = order.CreatedAt,
                ApprovedByName = order.ApprovedByUser?.FullName,
                ApprovedAt = order.ApprovedAt,
                AssignedToName = order.AssignedToUser?.FullName,
                AssignedToUserId = order.AssignedToUserId,
                ConfirmedByName = order.ConfirmedByUser?.FullName,
                ConfirmedAt = order.ConfirmedAt,
                InventoryPosted = inventoryPosted,
                CanEdit = order.Status == "DRAFT" && currentUserId == order.CreatedByUserId,
                CanCancel = (order.Status == "DRAFT" && currentUserId == order.CreatedByUserId) || (isManager && (order.Status == "DRAFT" || order.Status == "APPROVED")),
                CanApprove = order.Status == "DRAFT" && isManager,
                CanConfirm = order.Status == "APPROVED" && currentUserId == order.AssignedToUserId,
                TotalRequestedQuantity = order.TransferOrderDetails.Sum(d => d.RequestedQuantity),
                TotalMovedQuantity = order.TransferOrderDetails.Sum(d => d.MovedQuantity),
                Details = order.TransferOrderDetails.Select(d => new TransferOrderDetailItemDto
                {
                    TransferOrderDetailId = d.TransferOrderDetailId,
                    ProductId = d.ProductId,
                    ProductLotId = d.ProductLotId ?? 0,
                    ProductCode = d.Product?.ProductCode ?? "",
                    ProductName = d.Product?.ProductName ?? "",
                    UnitName = d.Product?.UnitOfMeasure?.UnitName ?? "",
                    QuantityScale = d.Product?.UnitOfMeasure?.QuantityScale ?? 0,
                    LotNumber = d.ProductLot?.LotNumber ?? "",
                    SourceLocationId = d.SourceLocationId ?? 0,
                    SourceLocationCode = d.SourceLocation?.LocationCode ?? "",
                    DestLocationId = d.DestinationLocationId ?? 0,
                    DestLocationCode = d.DestinationLocation?.LocationCode ?? "",
                    RequestedQuantity = d.RequestedQuantity,
                    MovedQuantity = d.MovedQuantity
                }).ToList()
            };
        }

        public async Task<TransferResultDto> CreateOrderAsync(long staffId, CreateTransferOrderDto dto)
        {
            var validationErrors = ValidateRequestShape(dto);
            if (validationErrors.Any())
                return new TransferResultDto { Success = false, Message = string.Join("\n", validationErrors) };

            var repoParams = dto.Items.Select(i => new TransferItemParam
            {
                ProductId = i.ProductId, ProductLotId = i.ProductLotId,
                SourceLocationId = i.SourceLocationId, DestLocationId = i.DestLocationId,
                Quantity = i.Quantity
            }).ToList();

            var itemErrors = await _repository.ValidateTransferItemsAsync(dto.WarehouseId, repoParams);
            if (itemErrors.Any())
                return new TransferResultDto { Success = false, Message = string.Join("\n", itemErrors) };

            var allocations = dto.Items.SelectMany(i => new[]
            {
                new CapacityAllocationDto { StorageLocationId = i.DestLocationId, ProductId = i.ProductId, Quantity = i.Quantity },
                new CapacityAllocationDto { StorageLocationId = i.SourceLocationId, ProductId = i.ProductId, Quantity = -i.Quantity }
            }).ToList();

            // Validate capacity without locking
            var evaluations = await _capacityService.EvaluateAsync(allocations, acquireLocationLocks: false);
            var destinationIds = dto.Items.Select(i => i.DestLocationId).ToHashSet();
            var exceededLocs = evaluations.Values.Where(e => destinationIds.Contains(e.StorageLocationId) &&
                e.OverallStatus == CapacityEvaluationStatuses.Exceeded).Select(e => e.LocationCode).ToList();
            if (exceededLocs.Any())
                return new TransferResultDto { Success = false, Message = $"Vị trí đích đã vượt quá sức chứa: {string.Join(", ", exceededLocs)}" };

            var hasUnverified = evaluations.Values.Any(e => destinationIds.Contains(e.StorageLocationId) &&
                (e.OverallStatus == CapacityEvaluationStatuses.Unknown || e.OverallStatus == CapacityEvaluationStatuses.NotConfigured));
            var notes = dto.Notes;
            if (hasUnverified)
                notes = string.IsNullOrWhiteSpace(notes) ? "[CAPACITY_UNVERIFIED]" : notes + "\n[CAPACITY_UNVERIFIED]";

            var order = await _repository.CreatePendingOrderAsync(dto.WarehouseId, dto.DueDate, repoParams, staffId, notes);
            await _auditLogService.RecordAsync(new AuditEventDto { UserId = staffId, ActionType = "CREATE_TRANSFER", EntityName = "TransferOrder", EntityId = order.TransferOrderId.ToString() });

            return new TransferResultDto { Success = true, Message = "Tạo phiếu thành công.", TransferOrderId = order.TransferOrderId, TransferOrderNumber = order.TransferOrderNumber };
        }

        public async Task<TransferResultDto> UpdateDraftOrderAsync(long staffId, UpdateTransferOrderDto dto)
        {
            var validationErrors = ValidateRequestShape(dto);
            if (validationErrors.Any())
                return new TransferResultDto { Success = false, Message = string.Join("\n", validationErrors) };

            var repoParams = dto.Items.Select(i => new TransferItemParam
            {
                ProductId = i.ProductId, ProductLotId = i.ProductLotId,
                SourceLocationId = i.SourceLocationId, DestLocationId = i.DestLocationId,
                Quantity = i.Quantity
            }).ToList();

            var itemErrors = await _repository.ValidateTransferItemsAsync(dto.WarehouseId, repoParams);
            if (itemErrors.Any())
                return new TransferResultDto { Success = false, Message = string.Join("\n", itemErrors) };

            var allocations = dto.Items.SelectMany(i => new[]
            {
                new CapacityAllocationDto { StorageLocationId = i.DestLocationId, ProductId = i.ProductId, Quantity = i.Quantity },
                new CapacityAllocationDto { StorageLocationId = i.SourceLocationId, ProductId = i.ProductId, Quantity = -i.Quantity }
            }).ToList();

            var evaluations = await _capacityService.EvaluateAsync(allocations, acquireLocationLocks: false);
            var destinationIds = dto.Items.Select(i => i.DestLocationId).ToHashSet();
            var exceededLocs = evaluations.Values.Where(e => destinationIds.Contains(e.StorageLocationId) &&
                e.OverallStatus == CapacityEvaluationStatuses.Exceeded).Select(e => e.LocationCode).ToList();
            if (exceededLocs.Any())
                return new TransferResultDto { Success = false, Message = $"Vị trí đích đã vượt quá sức chứa: {string.Join(", ", exceededLocs)}" };

            var order = await _repository.UpdateDraftOrderAsync(dto.TransferOrderId, dto.WarehouseId, dto.DueDate, repoParams, staffId, dto.Notes);
            await _auditLogService.RecordAsync(new AuditEventDto { UserId = staffId, ActionType = "UPDATE_TRANSFER", EntityName = "TransferOrder", EntityId = order.TransferOrderId.ToString() });

            return new TransferResultDto { Success = true, Message = "Cập nhật phiếu thành công." };
        }

        private static List<string> ValidateRequestShape(CreateTransferOrderDto dto)
        {
            var errors = new List<string>();
            if (dto.WarehouseId <= 0)
                errors.Add("Kho hàng không hợp lệ.");
            if (dto.Items == null || dto.Items.Count == 0)
                errors.Add("Vui lòng thêm ít nhất 1 sản phẩm.");
            if (dto.DueDate.HasValue && dto.DueDate.Value < DateOnly.FromDateTime(DateTime.UtcNow.Date))
                errors.Add("Ngày dự kiến hoàn thành không được nhỏ hơn hôm nay.");
            return errors;
        }
    }
}

