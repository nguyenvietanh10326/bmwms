using BMWMS.Business.DTOs.StockOperations;
using BMWMS.Business.Interfaces.StockOperations;
using BMWMS.Repository.Interfaces.StockOperations;

namespace BMWMS.Business.Services.StockOperations
{
    public class TransferService : ITransferService
    {
        private readonly ITransferRepository _transferRepo;

        public TransferService(ITransferRepository transferRepo)
        {
            _transferRepo = transferRepo;
        }

        // 1. Lấy danh sách hàng tồn trong ô nguồn
        public async Task<List<TransferInventoryItemDto>> GetLocationInventoryAsync(long locationId)
        {
            var items = await _transferRepo.GetInventoriesByLocationAsync(locationId);

            return items.Select(i => new TransferInventoryItemDto
            {
                InventoryId       = i.InventoryId,
                ProductId         = i.ProductId,
                ProductCode       = i.Product?.ProductCode ?? "N/A",
                ProductName       = i.Product?.ProductName ?? "N/A",
                UnitName          = i.Product?.UnitOfMeasure?.UnitName ?? "",
                ProductLotId      = i.ProductLotId,
                LotNumber         = i.ProductLot?.LotNumber ?? "N/A",
                ExpiryDate        = i.ProductLot?.ExpiryDate,
                OnHandQuantity    = i.OnHandQuantity,
                ReservedQuantity  = i.ReservedQuantity,
                AvailableQuantity = i.AvailableQuantity ?? (i.OnHandQuantity - i.ReservedQuantity)
            }).ToList();
        }

        // 2. Danh sách ô kho (dropdown)
        public async Task<List<LocationOptionDto>> GetLocationsForDropdownAsync(long warehouseId)
        {
            var locations = await _transferRepo.GetActiveLocationsByWarehouseAsync(warehouseId);

            return locations.Select(l => new LocationOptionDto
            {
                LocationId       = l.StorageLocationId,
                LocationCode     = l.LocationCode,
                LocationName     = l.LocationName ?? l.LocationCode,
                ZoneCode         = l.StorageRack?.WarehouseZone?.ZoneCode ?? "",
                RackCode         = l.StorageRack?.RackCode ?? "",
                IsPutawayAllowed = l.IsPutawayAllowed,
                Status           = l.Status
            }).ToList();
        }

        // 3. Validate ô đích
        public async Task<BinCapacityCheckDto> ValidateDestinationAsync(
            long destLocationId, long sourceLocationId, long productId)
        {
            if (destLocationId == sourceLocationId)
                return new BinCapacityCheckDto { IsValid = false, Message = "Ô đích không được trùng ô nguồn." };

            var destLocation = await _transferRepo.GetLocationWithInventoryAsync(destLocationId);

            if (destLocation == null)
                return new BinCapacityCheckDto { IsValid = false, Message = "Không tìm thấy ô đích." };

            if (destLocation.Status != "ACTIVE")
                return new BinCapacityCheckDto { IsValid = false, Message = $"Ô đích '{destLocation.LocationCode}' không ở trạng thái hoạt động." };

            if (!destLocation.IsPutawayAllowed)
                return new BinCapacityCheckDto { IsValid = false, Message = $"Ô đích '{destLocation.LocationCode}' không cho phép nhận hàng." };

            return new BinCapacityCheckDto { IsValid = true, Message = "Ô đích hợp lệ." };
        }

        // 4. Danh sách phiếu phân trang
        public async Task<TransferOrderPagedResultDto> GetPagedOrdersAsync(TransferOrderFilterDto filter)
        {
            var (items, totalCount, pendingCount, approvedCount, rejectedCount) =
                await _transferRepo.GetPagedOrdersAsync(filter.Keyword, filter.Status, filter.WarehouseId, filter.PageIndex, filter.PageSize);

            var list = items.Select(o =>
            {
                var (label, css) = GetStatusBadge(o.Status);
                var firstDetail = o.TransferOrderDetails.FirstOrDefault();

                return new TransferOrderListDto
                {
                    TransferOrderId     = o.TransferOrderId,
                    TransferOrderNumber = o.TransferOrderNumber,
                    TransferType        = o.TransferType,
                    WarehouseName       = o.SourceWarehouse?.WarehouseName ?? "N/A",
                    Status              = o.Status,
                    StatusLabel         = label,
                    StatusCss           = css,
                    CreatedByName       = o.CreatedByUser?.FullName ?? "N/A",
                    ConfirmedByName     = o.ConfirmedByUser?.FullName,
                    RequestedDate       = o.RequestedDate,
                    CreatedAt           = o.CreatedAt,
                    ConfirmedAt         = o.ConfirmedAt,
                    TotalItems          = o.TransferOrderDetails.Count,
                    Notes               = o.Notes
                };
            }).ToList();

            return new TransferOrderPagedResultDto
            {
                Items         = list,
                TotalCount    = totalCount,
                PageIndex     = filter.PageIndex,
                PageSize      = filter.PageSize,
                PendingCount  = pendingCount,
                ApprovedCount = approvedCount,
                RejectedCount = rejectedCount
            };
        }

        // 5. Chi tiết 1 phiếu
        public async Task<TransferOrderDetailViewDto?> GetOrderDetailAsync(long transferOrderId)
        {
            var order = await _transferRepo.GetOrderWithDetailsAsync(transferOrderId);
            if (order == null) return null;

            var (label, _) = GetStatusBadge(order.Status);

            return new TransferOrderDetailViewDto
            {
                TransferOrderId     = order.TransferOrderId,
                TransferOrderNumber = order.TransferOrderNumber,
                TransferType        = order.TransferType,
                Status              = order.Status,
                StatusLabel         = label,
                WarehouseName       = order.SourceWarehouse?.WarehouseName ?? "N/A",
                RequestedDate       = order.RequestedDate,
                Notes               = order.Notes,
                CreatedByName       = order.CreatedByUser?.FullName ?? "N/A",
                CreatedAt           = order.CreatedAt,
                ConfirmedByName     = order.ConfirmedByUser?.FullName,
                ConfirmedAt         = order.ConfirmedAt,
                Details = order.TransferOrderDetails.Select(d => new TransferOrderDetailItemDto
                {
                    TransferOrderDetailId = d.TransferOrderDetailId,
                    ProductCode           = d.Product?.ProductCode ?? "N/A",
                    ProductName           = d.Product?.ProductName ?? "N/A",
                    UnitName              = d.Product?.UnitOfMeasure?.UnitName ?? "",
                    LotNumber             = d.ProductLot?.LotNumber ?? "N/A",
                    SourceLocationCode    = d.SourceLocation?.LocationCode ?? "N/A",
                    DestLocationCode      = d.DestinationLocation?.LocationCode ?? "N/A",
                    RequestedQuantity     = d.RequestedQuantity,
                    MovedQuantity         = d.MovedQuantity
                }).ToList()
            };
        }

        // 6. Staff tạo phiếu (PENDING)
        public async Task<TransferResultDto> CreatePendingOrderAsync(CreateTransferOrderDto dto, long createdByUserId)
        {
            if (dto.Quantity <= 0)
                return new TransferResultDto { Success = false, Message = "Số lượng chuyển phải lớn hơn 0." };

            var srcLocation = await _transferRepo.GetLocationWithInventoryAsync(dto.SourceLocationId);
            if (srcLocation == null)
                return new TransferResultDto { Success = false, Message = "Không tìm thấy ô nguồn." };

            var srcInv = srcLocation.Inventories
                .FirstOrDefault(i => i.ProductId == dto.ProductId && i.ProductLotId == dto.ProductLotId);

            if (srcInv == null)
                return new TransferResultDto { Success = false, Message = "Sản phẩm / Lô hàng không tồn tại trong ô nguồn." };

            var available = srcInv.AvailableQuantity ?? (srcInv.OnHandQuantity - srcInv.ReservedQuantity);
            if (dto.Quantity > available)
                return new TransferResultDto
                {
                    Success = false,
                    Message = $"Số lượng khả dụng tại ô nguồn chỉ còn {available:N2} — không đủ để chuyển {dto.Quantity:N2}."
                };

            var destCheck = await ValidateDestinationAsync(dto.DestLocationId, dto.SourceLocationId, dto.ProductId);
            if (!destCheck.IsValid)
                return new TransferResultDto { Success = false, Message = destCheck.Message };

            try
            {
                var order = await _transferRepo.CreatePendingOrderAsync(
                    dto.WarehouseId,
                    dto.SourceLocationId,
                    dto.DestLocationId,
                    dto.ProductId,
                    dto.ProductLotId,
                    dto.Quantity,
                    createdByUserId,
                    dto.Notes);

                return new TransferResultDto
                {
                    Success = true,
                    Message = $"Đã tạo phiếu yêu cầu điều chuyển thành công (Mã phiếu: {order.TransferOrderNumber}). Đang chờ quản lý phê duyệt.",
                    TransferOrderId     = order.TransferOrderId,
                    TransferOrderNumber = order.TransferOrderNumber
                };
            }
            catch (Exception ex)
            {
                return new TransferResultDto { Success = false, Message = $"Lỗi hệ thống: {ex.Message}" };
            }
        }

        // 7. Manager Duyệt
        public async Task<TransferResultDto> ApproveOrderAsync(long transferOrderId, long approvedByUserId, string? notes)
        {
            try
            {
                var order = await _transferRepo.ApproveOrderAsync(transferOrderId, approvedByUserId, notes);
                return new TransferResultDto
                {
                    Success = true,
                    Message = $"Đã phê duyệt phiếu {order.TransferOrderNumber} thành công! Hàng hóa đã được điều chuyển trong kho.",
                    TransferOrderId     = order.TransferOrderId,
                    TransferOrderNumber = order.TransferOrderNumber
                };
            }
            catch (InvalidOperationException ex)
            {
                return new TransferResultDto { Success = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                return new TransferResultDto { Success = false, Message = $"Lỗi khi duyệt phiếu: {ex.Message}" };
            }
        }

        // 8. Manager Từ chối
        public async Task<TransferResultDto> RejectOrderAsync(long transferOrderId, long rejectedByUserId, string? notes)
        {
            try
            {
                var order = await _transferRepo.RejectOrderAsync(transferOrderId, rejectedByUserId, notes);
                return new TransferResultDto
                {
                    Success = true,
                    Message = $"Đã từ chối phiếu {order.TransferOrderNumber}.",
                    TransferOrderId     = order.TransferOrderId,
                    TransferOrderNumber = order.TransferOrderNumber
                };
            }
            catch (InvalidOperationException ex)
            {
                return new TransferResultDto { Success = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                return new TransferResultDto { Success = false, Message = $"Lỗi khi từ chối phiếu: {ex.Message}" };
            }
        }

        // ── Helper Badge Status ────────────────────────────────────────────────
        private static (string Label, string Css) GetStatusBadge(string status)
        {
            return status?.ToUpper() switch
            {
                "PENDING"   => ("Chờ phê duyệt", "bg-warning text-dark"),
                "COMPLETED" => ("Đã hoàn thành", "bg-success text-white"),
                "REJECTED"  => ("Đã từ chối", "bg-danger text-white"),
                _           => (status ?? "Khác", "bg-secondary text-white")
            };
        }
    }
}
