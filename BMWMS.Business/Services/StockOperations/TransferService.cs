using BMWMS.Business.DTOs.StockOperations;
using BMWMS.Business.Interfaces.StockOperations;
using BMWMS.Repository.Interfaces.StockOperations;
using BMWMS.Repository.Models;

namespace BMWMS.Business.Services.StockOperations
{
    public class TransferService : ITransferService
    {
        private readonly ITransferRepository _transferRepo;

        public TransferService(ITransferRepository transferRepo)
        {
            _transferRepo = transferRepo;
        }

        public async Task<List<ZoneOptionDto>> GetZonesAsync(long warehouseId = 1)
        {
            var zones = await _transferRepo.GetZonesByWarehouseAsync(warehouseId);
            return zones.Select(z => new ZoneOptionDto
            {
                ZoneId = z.ZoneId,
                ZoneCode = z.ZoneCode,
                ZoneName = z.ZoneName ?? z.ZoneCode
            }).ToList();
        }

        public async Task<List<RackOptionDto>> GetRacksAsync(long warehouseId = 1, long? zoneId = null)
        {
            var racks = await _transferRepo.GetRacksByZoneAsync(warehouseId, zoneId);
            return racks.Select(r => new RackOptionDto
            {
                RackId = r.RackId,
                RackCode = r.RackCode,
                RackName = r.RackName ?? r.RackCode,
                ZoneId = r.ZoneId
            }).ToList();
        }

        public async Task<List<TransferInventoryItemDto>> GetLocationInventoryAsync(long locationId)
        {
            var items = await _transferRepo.GetInventoriesByLocationAsync(locationId);
            return items.Select(i => new TransferInventoryItemDto
            {
                InventoryId = i.InventoryId,
                ProductId = i.ProductId,
                ProductCode = i.Product?.ProductCode ?? "N/A",
                ProductName = i.Product?.ProductName ?? "N/A",
                UnitName = i.Product?.UnitOfMeasure?.UnitName ?? "",
                ProductLotId = i.ProductLotId,
                LotNumber = i.ProductLot?.LotNumber ?? "N/A",
                ExpiryDate = i.ProductLot?.ExpiryDate,
                OnHandQuantity = i.OnHandQuantity,
                ReservedQuantity = i.ReservedQuantity,
                AvailableQuantity = i.AvailableQuantity ?? (i.OnHandQuantity - i.ReservedQuantity)
            }).ToList();
        }

        public async Task<List<LocationOptionDto>> GetLocationsForDropdownAsync(long warehouseId = 1, long? zoneId = null, long? rackId = null)
        {
            var locations = await _transferRepo.GetActiveLocationsByWarehouseAsync(warehouseId, zoneId, rackId);
            return locations.Select(l => new LocationOptionDto
            {
                LocationId = l.StorageLocationId,
                LocationCode = l.LocationCode,
                LocationName = l.LocationName ?? l.LocationCode,
                ZoneId = l.StorageRack?.ZoneId,
                ZoneCode = l.StorageRack?.WarehouseZone?.ZoneCode ?? "",
                RackId = l.RackId,
                RackCode = l.StorageRack?.RackCode ?? "",
                IsPutawayAllowed = l.IsPutawayAllowed,
                IsPickable = l.IsPickable,
                Status = l.Status
            }).ToList();
        }

        public async Task<BinCapacityCheckDto> ValidateDestinationAsync(long destLocationId, long sourceLocationId, long productId)
        {
            if (destLocationId == sourceLocationId)
                return new BinCapacityCheckDto { IsValid = false, Message = "O dich khong duoc trung o nguon." };

            var destLocation = await _transferRepo.GetLocationWithInventoryAsync(destLocationId);
            if (destLocation == null)
                return new BinCapacityCheckDto { IsValid = false, Message = "Khong tim thay o dich." };

            var status = destLocation.Status?.ToUpperInvariant();
            if (status != "ACTIVE" && status != "AVAILABLE" && status != "OCCUPIED")
                return new BinCapacityCheckDto { IsValid = false, Message = $"O dich '{destLocation.LocationCode}' khong o trang thai hoat dong." };

            if (!destLocation.IsPutawayAllowed)
                return new BinCapacityCheckDto { IsValid = false, Message = $"O dich '{destLocation.LocationCode}' khong cho phep nhan hang." };

            return new BinCapacityCheckDto { IsValid = true, Message = "O dich hop le." };
        }

        public async Task<List<StaffOptionDto>> GetStaffUsersAsync()
        {
            var users = await _transferRepo.GetStaffUsersAsync();
            return users.Select(u => new StaffOptionDto
            {
                UserId = u.UserId,
                FullName = u.FullName ?? u.Username,
                Username = u.Username,
                RoleCode = u.Role?.RoleCode ?? ""
            }).ToList();
        }

        public Task<List<TransferUseCaseDto>> GetUseCasesAsync()
        {
            var items = new List<TransferUseCaseDto>
            {
                new()
                {
                    Code = "TC-01",
                    Name = "Tao phieu chuyen kho noi bo",
                    Actor = "Warehouse Manager",
                    Preconditions = "Co hang tai o nguon va o dich hop le",
                    MainFlow = "Chon nguon, dich, san pham, lo, so luong -> he thong kiem tra -> luu DRAFT",
                    ResultStatus = "DRAFT",
                    ApiEndpoint = "POST /api/transfers/create"
                },
                new()
                {
                    Code = "TC-02",
                    Name = "Sua phieu truoc khi phe duyet",
                    Actor = "Warehouse Manager",
                    Preconditions = "Phieu dang DRAFT",
                    MainFlow = "Mo phieu DRAFT -> sua nguon/dich/san pham/lo/so luong/nhan vien/ngay du kien -> luu lai",
                    ResultStatus = "DRAFT",
                    ApiEndpoint = "PUT /api/transfers/{id}"
                },
                new()
                {
                    Code = "TC-03",
                    Name = "Phe duyet / tu choi phieu",
                    Actor = "Warehouse Manager",
                    Preconditions = "Phieu dang DRAFT",
                    MainFlow = "Duyet phieu va giao NV phu trach hoac tu choi phieu",
                    ResultStatus = "ASSIGNED / CANCELLED",
                    ApiEndpoint = "POST /api/transfers/{id}/approve | POST /api/transfers/{id}/reject"
                },
                new()
                {
                    Code = "TC-04",
                    Name = "Xac nhan xuat hang khoi o nguon",
                    Actor = "Warehouse Staff",
                    Preconditions = "Phieu da duyet",
                    MainFlow = "Nhan vien lay hang, xac nhan da xuat -> cap nhat trang thai IN_PROGRESS",
                    ResultStatus = "IN_PROGRESS",
                    ApiEndpoint = "POST /api/transfers/{id}/issue"
                },
                new()
                {
                    Code = "TC-05",
                    Name = "Xac nhan nhap hang vao o dich",
                    Actor = "Warehouse Staff",
                    Preconditions = "Phieu dang IN_PROGRESS",
                    MainFlow = "Xac nhan hang da ve dich -> he thong ghi TRANSFER_OUT/TRANSFER_IN -> hoan tat phieu",
                    ResultStatus = "COMPLETED",
                    ApiEndpoint = "POST /api/transfers/{id}/receive"
                }
            };

            return Task.FromResult(items);
        }

        public async Task<TransferOrderPagedResultDto> GetPagedOrdersAsync(TransferOrderFilterDto filter)
        {
            var (items, totalCount, draftCount, approvedCount, inProgressCount, completedCount, cancelledCount) =
                await _transferRepo.GetPagedOrdersAsync(filter.Keyword, filter.Status, filter.WarehouseId, filter.PageIndex, filter.PageSize);

            var list = items.Select(o =>
            {
                var (label, css) = GetStatusBadge(o.Status);
                var inventoryPosted = HasInventoryPosted(o);
                return new TransferOrderListDto
                {
                    TransferOrderId = o.TransferOrderId,
                    TransferOrderNumber = o.TransferOrderNumber,
                    TransferType = o.TransferType,
                    WarehouseName = o.SourceWarehouse?.WarehouseName ?? "N/A",
                    SourceLocationSummary = BuildLocationSummary(o.TransferOrderDetails.SelectMany(d => new[] { d.SourceLocation }).Where(x => x != null).Select(x => x!)),
                    DestinationLocationSummary = BuildLocationSummary(o.TransferOrderDetails.SelectMany(d => new[] { d.DestinationLocation }).Where(x => x != null).Select(x => x!)),
                    Status = o.Status,
                    StatusLabel = label,
                    StatusCss = css,
                    ProgressLabel = GetProgressLabel(o.Status, inventoryPosted),
                    NextAction = GetNextAction(o.Status, inventoryPosted),
                    CreatedByName = o.CreatedByUser?.FullName ?? "N/A",
                    AssignedToName = o.AssignedToUser?.FullName,
                    ConfirmedByName = o.ConfirmedByUser?.FullName,
                    RequestedDate = o.RequestedDate,
                    DueDate = o.DueDate,
                    CreatedAt = o.CreatedAt,
                    ConfirmedAt = o.ConfirmedAt,
                    TotalItems = o.TransferOrderDetails.Count,
                    TotalRequestedQuantity = o.TransferOrderDetails.Sum(d => d.RequestedQuantity),
                    TotalMovedQuantity = o.TransferOrderDetails.Sum(d => d.MovedQuantity),
                    InventoryPosted = inventoryPosted,
                    Notes = o.Notes
                };
            }).ToList();

            return new TransferOrderPagedResultDto
            {
                Items = list,
                TotalCount = totalCount,
                PageIndex = filter.PageIndex,
                PageSize = filter.PageSize,
                DraftCount = draftCount,
                ApprovedCount = approvedCount,
                InProgressCount = inProgressCount,
                CompletedCount = completedCount,
                CancelledCount = cancelledCount
            };
        }

        public async Task<TransferOrderDetailViewDto?> GetOrderDetailAsync(long transferOrderId)
        {
            var order = await _transferRepo.GetOrderWithDetailsAsync(transferOrderId);
            if (order == null) return null;

            var (label, css) = GetStatusBadge(order.Status);
            var inventoryPosted = HasInventoryPosted(order);
            var totalRequested = order.TransferOrderDetails.Sum(d => d.RequestedQuantity);
            var totalMoved = order.TransferOrderDetails.Sum(d => d.MovedQuantity);
            var linkedInbound = order.InboundOrders.FirstOrDefault();
            var linkedOutbound = order.OutboundOrders.FirstOrDefault();
            var isWorkflowLinked = linkedInbound != null || linkedOutbound != null;

            return new TransferOrderDetailViewDto
            {
                TransferOrderId = order.TransferOrderId,
                TransferOrderNumber = order.TransferOrderNumber,
                TransferType = order.TransferType,
                Status = order.Status,
                StatusLabel = label,
                ProgressLabel = GetProgressLabel(order.Status, inventoryPosted),
                NextAction = GetNextAction(order.Status, inventoryPosted),
                WarehouseName = order.SourceWarehouse?.WarehouseName ?? "N/A",
                RequestedDate = order.RequestedDate,
                DueDate = order.DueDate,
                Notes = order.Notes,
                CreatedByName = order.CreatedByUser?.FullName ?? "N/A",
                CreatedAt = order.CreatedAt,
                AssignedToName = order.AssignedToUser?.FullName,
                AssignedToUserId = order.AssignedToUserId,
                ConfirmedByName = order.ConfirmedByUser?.FullName,
                ConfirmedAt = order.ConfirmedAt,
                InventoryPosted = inventoryPosted,
                IsWorkflowLinked = isWorkflowLinked,
                ParentDocumentType = linkedInbound != null ? "INBOUND" : linkedOutbound != null ? "OUTBOUND" : null,
                ParentDocumentId = linkedInbound?.InboundOrderId ?? linkedOutbound?.OutboundOrderId,
                ParentDocumentNumber = linkedInbound?.InboundOrderNumber ?? linkedOutbound?.OutboundOrderNumber,
                CanEdit = !isWorkflowLinked && order.Status == "DRAFT",
                CanApprove = !isWorkflowLinked && order.Status == "DRAFT",
                CanReject = !isWorkflowLinked && (order.Status == "DRAFT" || order.Status == "APPROVED" || order.Status == "ASSIGNED"),
                CanIssue = !isWorkflowLinked && (order.Status == "APPROVED" || order.Status == "ASSIGNED"),
                CanReceive = !isWorkflowLinked && order.Status == "IN_PROGRESS" && !inventoryPosted,
                TotalRequestedQuantity = totalRequested,
                TotalMovedQuantity = totalMoved,
                Details = order.TransferOrderDetails.Select(d => new TransferOrderDetailItemDto
                {
                    TransferOrderDetailId = d.TransferOrderDetailId,
                    ProductId = d.ProductId,
                    ProductLotId = d.ProductLotId ?? 0,
                    ProductCode = d.Product?.ProductCode ?? "N/A",
                    ProductName = d.Product?.ProductName ?? "N/A",
                    UnitName = d.Product?.UnitOfMeasure?.UnitName ?? "",
                    LotNumber = d.ProductLot?.LotNumber ?? "N/A",
                    SourceLocationId = d.SourceLocationId ?? 0,
                    SourceZoneId = d.SourceLocation?.StorageRack?.ZoneId,
                    SourceRackId = d.SourceLocation?.RackId,
                    SourceZoneCode = d.SourceLocation?.StorageRack?.WarehouseZone?.ZoneCode ?? "",
                    SourceRackCode = d.SourceLocation?.StorageRack?.RackCode ?? "",
                    SourceLocationCode = d.SourceLocation?.LocationCode ?? "N/A",
                    DestLocationId = d.DestinationLocationId ?? 0,
                    DestZoneId = d.DestinationLocation?.StorageRack?.ZoneId,
                    DestRackId = d.DestinationLocation?.RackId,
                    DestZoneCode = d.DestinationLocation?.StorageRack?.WarehouseZone?.ZoneCode ?? "",
                    DestRackCode = d.DestinationLocation?.StorageRack?.RackCode ?? "",
                    DestLocationCode = d.DestinationLocation?.LocationCode ?? "N/A",
                    RequestedQuantity = d.RequestedQuantity,
                    MovedQuantity = d.MovedQuantity
                }).ToList()
            };
        }

        public async Task<TransferResultDto> CreatePendingOrderAsync(CreateTransferOrderDto dto, long createdByUserId)
        {
            if (dto.Items == null || !dto.Items.Any())
                return new TransferResultDto { Success = false, Message = "Danh sach hang chuyen khong duoc rong." };

            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0)
                    return new TransferResultDto { Success = false, Message = "So luong chuyen phai lon hon 0." };

                if (item.ProductLotId <= 0)
                    return new TransferResultDto { Success = false, Message = "Phieu chuyen kho bat buoc co lo hang." };

                var destCheck = await ValidateDestinationAsync(item.DestLocationId, item.SourceLocationId, item.ProductId);
                if (!destCheck.IsValid)
                    return new TransferResultDto { Success = false, Message = destCheck.Message };
            }

            try
            {
                var repoParams = dto.Items.Select(i => new TransferItemParam
                {
                    SourceLocationId = i.SourceLocationId,
                    DestLocationId = i.DestLocationId,
                    ProductId = i.ProductId,
                    ProductLotId = i.ProductLotId,
                    Quantity = i.Quantity
                }).ToList();

                var order = await _transferRepo.CreatePendingOrderAsync(
                    dto.WarehouseId > 0 ? dto.WarehouseId : 1,
                    dto.AssignedToUserId,
                    dto.DueDate,
                    repoParams,
                    createdByUserId,
                    dto.Notes);

                return new TransferResultDto
                {
                    Success = true,
                    Message = $"Da tao lenh dieu chuyen {order.TransferOrderNumber} gom {dto.Items.Count} mat hang. Dang cho duyet.",
                    TransferOrderId = order.TransferOrderId,
                    TransferOrderNumber = order.TransferOrderNumber
                };
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} ({ex.InnerException.Message})" : ex.Message;
                return new TransferResultDto { Success = false, Message = $"Loi he thong: {msg}" };
            }
        }

        public async Task<TransferResultDto> UpdateDraftOrderAsync(long transferOrderId, UpdateTransferOrderDto dto, long updatedByUserId)
        {
            if (dto.Items == null || !dto.Items.Any())
                return new TransferResultDto { Success = false, Message = "Danh sach hang chuyen khong duoc rong." };

            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0)
                    return new TransferResultDto { Success = false, Message = "So luong chuyen phai lon hon 0." };

                if (item.ProductLotId <= 0)
                    return new TransferResultDto { Success = false, Message = "Phieu chuyen kho bat buoc co lo hang." };

                var destCheck = await ValidateDestinationAsync(item.DestLocationId, item.SourceLocationId, item.ProductId);
                if (!destCheck.IsValid)
                    return new TransferResultDto { Success = false, Message = destCheck.Message };
            }

            try
            {
                var repoParams = dto.Items.Select(i => new TransferItemParam
                {
                    SourceLocationId = i.SourceLocationId,
                    DestLocationId = i.DestLocationId,
                    ProductId = i.ProductId,
                    ProductLotId = i.ProductLotId,
                    Quantity = i.Quantity
                }).ToList();

                var order = await _transferRepo.UpdateDraftOrderAsync(
                    transferOrderId,
                    dto.WarehouseId > 0 ? dto.WarehouseId : 1,
                    dto.AssignedToUserId,
                    dto.DueDate,
                    repoParams,
                    updatedByUserId,
                    dto.Notes);

                return new TransferResultDto
                {
                    Success = true,
                    Message = $"Da cap nhat phieu {order.TransferOrderNumber}. Dang cho duyet.",
                    TransferOrderId = order.TransferOrderId,
                    TransferOrderNumber = order.TransferOrderNumber
                };
            }
            catch (InvalidOperationException ex)
            {
                return new TransferResultDto { Success = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message} ({ex.InnerException.Message})" : ex.Message;
                return new TransferResultDto { Success = false, Message = $"Loi he thong: {msg}" };
            }
        }

        public async Task<TransferResultDto> ApproveOrderAsync(long transferOrderId, long approvedByUserId, ApproveTransferDto dto)
        {
            try
            {
                var order = await _transferRepo.ApproveOrderAsync(transferOrderId, approvedByUserId, dto.AssignedToUserId, dto.Notes);
                return new TransferResultDto
                {
                    Success = true,
                    Message = $"Da phe duyet phieu {order.TransferOrderNumber}. Nhan vien kho se xuat hang theo lenh.",
                    TransferOrderId = order.TransferOrderId,
                    TransferOrderNumber = order.TransferOrderNumber
                };
            }
            catch (InvalidOperationException ex)
            {
                return new TransferResultDto { Success = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                return new TransferResultDto { Success = false, Message = $"Loi khi duyet phieu: {ex.Message}" };
            }
        }

        public async Task<TransferResultDto> RejectOrderAsync(long transferOrderId, long rejectedByUserId, string? notes)
        {
            try
            {
                var order = await _transferRepo.RejectOrderAsync(transferOrderId, rejectedByUserId, notes);
                return new TransferResultDto
                {
                    Success = true,
                    Message = $"Da tu choi phieu {order.TransferOrderNumber}.",
                    TransferOrderId = order.TransferOrderId,
                    TransferOrderNumber = order.TransferOrderNumber
                };
            }
            catch (InvalidOperationException ex)
            {
                return new TransferResultDto { Success = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                return new TransferResultDto { Success = false, Message = $"Loi khi tu choi phieu: {ex.Message}" };
            }
        }

        public async Task<TransferResultDto> ConfirmTransferIssueAsync(long transferOrderId, long staffUserId, string? notes)
        {
            try
            {
                var order = await _transferRepo.ConfirmTransferIssueAsync(transferOrderId, staffUserId, notes);
                return new TransferResultDto
                {
                    Success = true,
                    Message = $"Da xac nhan xuat phieu {order.TransferOrderNumber}.",
                    TransferOrderId = order.TransferOrderId,
                    TransferOrderNumber = order.TransferOrderNumber
                };
            }
            catch (InvalidOperationException ex)
            {
                return new TransferResultDto { Success = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                return new TransferResultDto { Success = false, Message = $"Loi khi xac nhan xuat: {ex.Message}" };
            }
        }

        public async Task<TransferResultDto> ConfirmTransferReceiptAsync(long transferOrderId, long staffUserId, string? notes)
        {
            try
            {
                var order = await _transferRepo.ConfirmTransferReceiptAsync(transferOrderId, staffUserId, notes);
                return new TransferResultDto
                {
                    Success = true,
                    Message = $"Da xac nhan nhap va hoan tat phieu {order.TransferOrderNumber}.",
                    TransferOrderId = order.TransferOrderId,
                    TransferOrderNumber = order.TransferOrderNumber
                };
            }
            catch (InvalidOperationException ex)
            {
                return new TransferResultDto { Success = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                return new TransferResultDto { Success = false, Message = $"Loi khi xac nhan nhap: {ex.Message}" };
            }
        }

        public async Task<TransferResultDto> ConfirmTransferAsync(long transferOrderId, long staffUserId, string? notes)
        {
            try
            {
                var order = await _transferRepo.ConfirmTransferAsync(transferOrderId, staffUserId, notes);
                return new TransferResultDto
                {
                    Success = true,
                    Message = $"Da hoan tat luon phieu {order.TransferOrderNumber}.",
                    TransferOrderId = order.TransferOrderId,
                    TransferOrderNumber = order.TransferOrderNumber
                };
            }
            catch (InvalidOperationException ex)
            {
                return new TransferResultDto { Success = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                return new TransferResultDto { Success = false, Message = $"Loi khi xac nhan: {ex.Message}" };
            }
        }

        private static (string Label, string Css) GetStatusBadge(string status)
        {
            return status?.ToUpperInvariant() switch
            {
                "DRAFT" => ("Cho duyet", "bg-warning text-dark"),
                "ASSIGNED" or "APPROVED" => ("Da duyet - Cho xuat", "bg-info text-white"),
                "IN_PROGRESS" => ("Dang xuat / dang nhap", "bg-primary text-white"),
                "COMPLETED" => ("Da hoan thanh", "bg-success text-white"),
                "CANCELLED" => ("Da huy", "bg-danger text-white"),
                _ => (status ?? "Khac", "bg-secondary text-white")
            };
        }

        private static string GetProgressLabel(string status, bool inventoryPosted)
        {
            return status?.ToUpperInvariant() switch
            {
                "DRAFT" => "Dang soan thao",
                "ASSIGNED" or "APPROVED" => "Da duyet - cho xuat",
                "IN_PROGRESS" when inventoryPosted => "Da nhap - hoan thanh",
                "IN_PROGRESS" => "Da xac nhan xuat - dang di chuyen",
                "COMPLETED" => "Da nhap - hoan thanh",
                "CANCELLED" => "Da huy",
                _ => "Khac"
            };
        }

        private static string GetNextAction(string status, bool inventoryPosted)
        {
            return status?.ToUpperInvariant() switch
            {
                "DRAFT" => "Manager duyet hoac tu choi",
                "ASSIGNED" or "APPROVED" => "Staff xac nhan xuat",
                "IN_PROGRESS" when inventoryPosted => "Khong con thao tac",
                "IN_PROGRESS" => "Staff xac nhan nhap",
                "COMPLETED" => "Khong con thao tac",
                "CANCELLED" => "Khong con thao tac",
                _ => "Khong xac dinh"
            };
        }

        private static bool HasInventoryPosted(TransferOrder order)
        {
            return order.TransferOrderDetails.Any() &&
                   order.TransferOrderDetails.All(d =>
                       d.InventoryTransactions.Any(t => t.TransactionType == "TRANSFER_OUT") &&
                       d.InventoryTransactions.Any(t => t.TransactionType == "TRANSFER_IN"));
        }

        private static string BuildLocationSummary(IEnumerable<StorageLocation> locations)
        {
            var codes = locations
                .Select(l => l.LocationCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct()
                .ToList();

            if (codes.Count == 0)
                return "N/A";

            return codes.Count <= 2
                ? string.Join(", ", codes)
                : $"{codes[0]}, {codes[1]} (+{codes.Count - 2})";
        }
    }
}
