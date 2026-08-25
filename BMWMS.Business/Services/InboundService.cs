using System;
using System.Linq;
using System.Threading.Tasks;
using BMWMS.Business.DTOs.Inbound;
using BMWMS.Business.Interfaces;
using BMWMS.Business.Common;
using BMWMS.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace BMWMS.Business.Services;

public class InboundService : IInboundService
{
    private readonly IInboundRepository _inboundRepository;
    private readonly BMWMS.Repository.Models.BmwmsContext _context;
    private readonly INotificationService _notificationService;

    public InboundService(IInboundRepository inboundRepository, BMWMS.Repository.Models.BmwmsContext context, INotificationService notificationService)
    {
        _inboundRepository = inboundRepository;
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<InboundOrderPageDto> GetInboundOrdersPageAsync(InboundOrderFilterDto filter)
    {
        var result = await _inboundRepository.GetInboundOrdersPageAsync(
            filter.Keyword,
            filter.Status,
            filter.FromDate,
            filter.ToDate,
            filter.AssignedToUserId,
            filter.PageIndex,
            filter.PageSize);

        return new InboundOrderPageDto
        {
            TotalCount = result.TotalCount,
            Items = result.Items.Select(x => new InboundOrderListDto
            {
                InboundOrderId = x.InboundOrderId,
                InboundOrderNumber = x.InboundOrderNumber,
                SupplierName = x.SourceType == "SALES_RETURN"
                    ? x.SalesOrder?.Customer?.CustomerName ?? "Không xác định"
                    : x.PurchaseOrder?.Supplier?.SupplierName ?? "Không xác định",
                SourceType = x.SourceType,
                SourceReference = x.SourceType == "SALES_RETURN"
                    ? x.SalesOrder?.SalesOrderNumber ?? string.Empty
                    : x.PurchaseOrder?.PurchaseOrderNumber ?? string.Empty,
                ExpectedReceiptDate = x.ExpectedReceiptDate,
                Status = GetInboundDisplayStatus(x),
                TotalExpectedQuantity = x.InboundOrderItems.Sum(i => i.ExpectedQuantity),
                TotalReceivedQuantity = x.InboundOrderItems.Sum(i => i.ReceivedQuantity),
                LineCount = x.InboundOrderItems.Count,
                IsSupplemental = x.ParentInboundOrderId.HasValue,
                CreatedAt = x.CreatedAt
            }).ToList()
        };
    }

    public async Task<InboundOrderDetailDto?> GetInboundOrderByIdAsync(long id)
    {
        var order = await _inboundRepository.GetByIdAsync(id);
        if (order == null) return null;

        var dto = new InboundOrderDetailDto
        {
            InboundOrderId = order.InboundOrderId,
            InboundOrderNumber = order.InboundOrderNumber,
            PurchaseOrderNumber = order.PurchaseOrder?.PurchaseOrderNumber,
            PurchaseOrderId = order.PurchaseOrderId,
            SalesOrderNumber = order.SalesOrder?.SalesOrderNumber,
            SalesOrderId = order.SalesOrderId,
            SourceType = order.SourceType,
            SourceReference = order.SourceType == "SALES_RETURN"
                ? order.SalesOrder?.SalesOrderNumber ?? string.Empty
                : order.PurchaseOrder?.PurchaseOrderNumber ?? string.Empty,
            PartnerName = order.SourceType == "SALES_RETURN"
                ? order.SalesOrder?.Customer?.CustomerName ?? string.Empty
                : order.PurchaseOrder?.Supplier?.SupplierName ?? string.Empty,
            SupplierName = order.SourceType == "SALES_RETURN"
                ? order.SalesOrder?.Customer?.CustomerName ?? string.Empty
                : order.PurchaseOrder?.Supplier?.SupplierName ?? string.Empty,
            WarehouseName = order.Warehouse?.WarehouseName ?? "",
            WarehouseId = order.WarehouseId,
            ExpectedReceiptDate = order.ExpectedReceiptDate,
            Status = GetInboundDisplayStatus(order),
            AssignedToUserId = order.AssignedToUserId,
            AssignedToUserName = order.AssignedToUser?.FullName ?? "",
            CreatedByUserName = order.CreatedByUser?.FullName ?? "",
            Notes = order.Notes,
            CancellationReason = order.CancellationReason,
            ParentInboundOrderNumber = order.ParentInboundOrder?.InboundOrderNumber,
            ParentInboundOrderId = order.ParentInboundOrderId,
            Items = order.InboundOrderItems.OrderBy(i => i.InboundOrderItemId).Select(i => new InboundOrderItemDto
            {
                InboundOrderItemId = i.InboundOrderItemId,
                ProductId = i.ProductId,
                ProductCode = i.Product.ProductCode,
                ProductName = i.Product.ProductName,
                UnitName = i.Product.UnitOfMeasure?.UnitName ?? string.Empty,
                QuantityScale = 0,
                TrackLot = i.Product.TrackLot,
                TrackExpiry = i.Product.TrackExpiry,
                ExpectedQuantity = i.ExpectedQuantity,
                ReceivedQuantity = i.ReceivedQuantity,
                PutawayQuantity = i.InboundOrderDetails
                    .Where(d => d.InventoryTransaction != null && d.InventoryTransaction.TransactionType == "INBOUND")
                    .Sum(d => d.ReceivedQuantity),
                DamagedQuantity = i.DamagedQuantity,
                ShortageQuantity = i.ShortageQuantity,
                SupplementalRemainingQuantity = Math.Max(0, i.ShortageQuantity - order.InverseParentInboundOrder
                    .Where(child => child.Status != "CANCELLED")
                    .SelectMany(child => child.InboundOrderItems)
                    .Where(childItem => childItem.ProductId == i.ProductId)
                    .Sum(childItem => childItem.ExpectedQuantity)),
                LotNumber = i.InboundOrderDetails.FirstOrDefault()?.ProductLot?.LotNumber,
                ExpiryDate = i.InboundOrderDetails.FirstOrDefault()?.ProductLot?.ExpiryDate,
                Receipts = i.InboundOrderDetails.Select(d => new BMWMS.Business.DTOs.Inbound.InboundReceiptDto
                {
                    InboundOrderDetailId = d.InboundOrderDetailId,
                    ProductLotId = d.ProductLotId,
                    LotNumber = d.ProductLot?.LotNumber ?? string.Empty,
                    ExpiryDate = d.ProductLot?.ExpiryDate,
                    ManufactureDate = d.ProductLot?.ManufactureDate,
                    ReceivedQuantity = d.ReceivedQuantity,
                    PutawayQuantity = d.InventoryTransaction?.TransactionType == "INBOUND" ? d.ReceivedQuantity : 0,
                    ConditionStatus = d.ConditionStatus,
                    LocationCode = d.StorageLocation?.LocationCode ?? string.Empty,
                    ZoneCode = d.StorageLocation?.StorageRack?.WarehouseZone?.ZoneCode ?? string.Empty,
                    RackCode = d.StorageLocation?.StorageRack?.RackCode ?? string.Empty,
                    RecordedByUserName = d.RecordedByUser?.FullName ?? string.Empty,
                    RecordedAt = d.RecordedAt,
                    Notes = d.Notes,
                    PutawayByUserName = d.InventoryTransaction?.PerformedByUser?.FullName ?? string.Empty,
                    PutawayAt = d.InventoryTransaction?.TransactionAt
                }).ToList()
            }).ToList()
        };

        var timeline = new List<InboundOrderTimelineDto>();
        
        timeline.Add(new InboundOrderTimelineDto
        {
            EventTime = order.CreatedAt,
            EventName = "Tạo lệnh nhập",
            StatusBadge = order.ConfirmedAt.HasValue ? "READY" : "DRAFT",
            StatusBadgeColor = "gy",
            PerformedBy = order.CreatedByUser?.FullName ?? "Hệ thống"
        });

        if (order.ConfirmedAt.HasValue)
        {
            timeline.Add(new InboundOrderTimelineDto
            {
                EventTime = order.ConfirmedAt.Value,
                EventName = "Xác nhận lệnh nhập",
                StatusBadge = "READY",
                StatusBadgeColor = "b",
                PerformedBy = order.ConfirmedByUser?.FullName ?? "Quản lý"
            });
        }
        
        var receiptDetails = order.InboundOrderItems.SelectMany(i => i.InboundOrderDetails).ToList();
        if (receiptDetails.Count > 0)
        {
            timeline.Add(new InboundOrderTimelineDto
            {
                EventTime = receiptDetails.Min(d => d.RecordedAt),
                EventName = "Bắt đầu kiểm nhận hàng",
                StatusBadge = "RECEIVING",
                StatusBadgeColor = "b",
                PerformedBy = receiptDetails.OrderBy(d => d.RecordedAt).First().RecordedByUser?.FullName ?? order.AssignedToUser?.FullName ?? ""
            });
        }

        if (order.Status == "COMPLETED")
        {
            timeline.Add(new InboundOrderTimelineDto
            {
                EventTime = receiptDetails.Count > 0 ? receiptDetails.Max(d => d.RecordedAt) : order.ConfirmedAt ?? order.CreatedAt,
                EventName = "Hoàn tất kiểm nhận",
                StatusBadge = "RECEIVED",
                StatusBadgeColor = "g",
                PerformedBy = order.AssignedToUser?.FullName ?? ""
            });

            var postedTransactions = receiptDetails.Where(d => d.InventoryTransaction != null)
                .Select(d => d.InventoryTransaction!).ToList();
            if (postedTransactions.Count > 0)
            {
                timeline.Add(new InboundOrderTimelineDto
                {
                    EventTime = postedTransactions.Max(t => t.TransactionAt),
                    EventName = postedTransactions.Count == receiptDetails.Count(d => d.ConditionStatus == "GOOD")
                        ? "Hoàn tất xếp hàng vào vị trí kho"
                        : "Xếp một phần hàng vào vị trí kho",
                    StatusBadge = postedTransactions.Count == receiptDetails.Count(d => d.ConditionStatus == "GOOD")
                        ? "PUTAWAY_COMPLETED"
                        : "RECEIVED",
                    StatusBadgeColor = "g",
                    PerformedBy = postedTransactions.OrderByDescending(t => t.TransactionAt).First().PerformedByUser?.FullName ?? order.AssignedToUser?.FullName ?? ""
                });
            }
        }
        
        if (order.CancelledAt.HasValue)
        {
            timeline.Add(new InboundOrderTimelineDto
            {
                EventTime = order.CancelledAt.Value,
                EventName = "Hủy lệnh nhập",
                StatusBadge = "CANCELLED",
                StatusBadgeColor = "r",
                PerformedBy = order.CancelledByUser?.FullName ?? ""
            });
        }

        // Sắp xếp giảm dần theo thời gian (mới nhất lên trên)
        dto.Timeline = timeline.OrderByDescending(t => t.EventTime).ToList();

        return dto;
    }

    public async Task<long> CreateInboundOrderAsync(CreateInboundOrderDto dto, long currentUserId)
    {
        dto.SourceType = (dto.SourceType ?? string.Empty).Trim().ToUpperInvariant();
        await using var assignmentTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        if (dto.Items == null || !dto.Items.Any())
            throw new ArgumentException("Lệnh nhập kho phải có ít nhất 1 dòng hàng.");

        if (dto.Items.Any(i => i.ExpectedQuantity <= 0))
            throw new ArgumentException("Số lượng dự kiến phải lớn hơn 0.");

        if (dto.Items.GroupBy(i => i.ProductId).Any(group => group.Count() > 1))
            throw new ArgumentException("Mỗi sản phẩm chỉ được xuất hiện một lần trong phiếu nhập kho.");

        var activeWarehouses = await _context.Warehouses
            .Where(w => w.Status == "ACTIVE")
            .OrderBy(w => w.WarehouseId)
            .Take(2)
            .ToListAsync();
        if (activeWarehouses.Count != 1)
            throw new InvalidOperationException("Hệ thống một kho phải có đúng một kho đang hoạt động.");
        dto.WarehouseId = activeWarehouses[0].WarehouseId;

        if (dto.IsSubmit && !dto.AssignedToUserId.HasValue)
            throw new ArgumentException("Vui lòng phân công nhân viên kho trước khi chuyển phiếu sang trạng thái Sẵn sàng.");
        await EnsureWarehouseStaffAssigneeAsync(dto.AssignedToUserId);
        await ValidateQuantitiesAsync(dto.Items.Select(i => (i.ProductId, i.ExpectedQuantity, "Số lượng dự kiến")));

        BMWMS.Repository.Models.InboundOrder? parentInbound = null;
        Dictionary<long, decimal>? supplementalBalances = null;
        if (dto.ParentInboundOrderId.HasValue)
        {
            parentInbound = await _context.InboundOrders
                .Include(o => o.InboundOrderItems)
                .FirstOrDefaultAsync(o => o.InboundOrderId == dto.ParentInboundOrderId.Value)
                ?? throw new ArgumentException("Không tìm thấy phiếu nhập gốc cần bổ sung.");
            if (parentInbound.SourceType != "PURCHASE_ORDER" || parentInbound.Status != "COMPLETED" ||
                !parentInbound.PurchaseOrderId.HasValue)
                throw new ArgumentException("Chỉ được tạo phiếu bổ sung từ phiếu nhập PO đã hoàn tất kiểm nhận.");

            dto.SourceType = "PURCHASE_ORDER";
            dto.PurchaseOrderId = parentInbound.PurchaseOrderId;
            dto.SalesOrderId = null;

            var allocatedSupplemental = await _context.InboundOrderItems
                .Where(i => i.InboundOrder.ParentInboundOrderId == parentInbound.InboundOrderId &&
                            i.InboundOrder.Status != "CANCELLED")
                .GroupBy(i => i.ProductId)
                .Select(group => new { ProductId = group.Key, Quantity = group.Sum(i => i.ExpectedQuantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Quantity);

            supplementalBalances = parentInbound.InboundOrderItems.ToDictionary(
                item => item.ProductId,
                item => Math.Max(0, item.ShortageQuantity -
                    (allocatedSupplemental.TryGetValue(item.ProductId, out var allocated) ? allocated : 0)));
            foreach (var item in dto.Items)
            {
                if (!supplementalBalances.TryGetValue(item.ProductId, out var balance) || item.ExpectedQuantity > balance)
                    throw new ArgumentException("Số lượng phiếu bổ sung vượt phần thiếu/hỏng chưa được lập phiếu thay thế.");
            }
        }

        if (dto.SourceType == "PURCHASE_ORDER")
        {
            if (!dto.PurchaseOrderId.HasValue)
                throw new ArgumentException("Vui lòng chọn PO cần nhập hàng.");

            var po = await GetPurchaseOrderForInboundAsync(dto.PurchaseOrderId.Value);
            if (po == null)
                throw new ArgumentException("PO không ở trạng thái đủ điều kiện hoặc không còn số lượng để lập phiếu nhập.");

            dto.SalesOrderId = null;
            foreach (var item in dto.Items)
            {
                var poItem = po?.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (poItem == null ||
                    (!dto.ParentInboundOrderId.HasValue && item.ExpectedQuantity > poItem.RemainingQuantity))
                    throw new ArgumentException("Số lượng dự kiến vượt quá số lượng PO còn lại.");
            }
        }
        else if (dto.SourceType == "SALES_RETURN")
        {
            if (!dto.SalesOrderId.HasValue)
                throw new ArgumentException("Vui lòng chọn SO có hàng khách trả.");

            var salesOrder = await GetSalesOrderForInboundAsync(dto.SalesOrderId.Value);
            if (salesOrder == null)
                throw new ArgumentException("SO chưa phát sinh xuất hàng hoặc không còn số lượng có thể trả.");

            dto.PurchaseOrderId = null;
            foreach (var item in dto.Items)
            {
                var salesItem = salesOrder.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (salesItem == null || item.ExpectedQuantity > salesItem.RemainingQuantity)
                    throw new ArgumentException("Số lượng khách trả vượt quá số lượng đã xuất còn có thể trả.");
            }
        }
        else
        {
            throw new ArgumentException("Loại nguồn phiếu nhập kho không được hỗ trợ.");
        }

        var inboundOrder = new BMWMS.Repository.Models.InboundOrder
        {
            InboundOrderNumber = "IN-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff"),
            SourceType = dto.SourceType,
            PurchaseOrderId = dto.PurchaseOrderId,
            SalesOrderId = dto.SalesOrderId,
            WarehouseId = dto.WarehouseId,
            ExpectedReceiptDate = dto.ExpectedReceiptDate,
            Notes = dto.Notes,
            Status = dto.IsSubmit ? "ASSIGNED" : "DRAFT",
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = currentUserId,
            AssignedToUserId = dto.AssignedToUserId,
            ConfirmedAt = dto.IsSubmit ? DateTime.UtcNow : null,
            ConfirmedByUserId = dto.IsSubmit ? currentUserId : null,
            ParentInboundOrderId = parentInbound?.InboundOrderId
        };

        foreach (var itemDto in dto.Items)
        {
            inboundOrder.InboundOrderItems.Add(new BMWMS.Repository.Models.InboundOrderItem
            {
                ProductId = itemDto.ProductId,
                ExpectedQuantity = itemDto.ExpectedQuantity,
                ReceivedQuantity = 0,
                DamagedQuantity = 0,
                ShortageQuantity = 0,
                Notes = itemDto.Notes
            });
        }

        await _inboundRepository.AddAsync(inboundOrder);
        await _inboundRepository.SaveChangesAsync();
        await assignmentTransaction.CommitAsync();

        if (inboundOrder.Status == "ASSIGNED" && inboundOrder.AssignedToUserId.HasValue)
        {
            await _notificationService.CreateNotificationAsync(new BMWMS.Business.DTOs.Notification.CreateNotificationDto
            {
                Title = "Bạn được giao một lệnh nhập kho mới",
                Message = $"Lệnh nhập kho {inboundOrder.InboundOrderNumber} đã được giao cho bạn. Vui lòng kiểm tra.",
                NotificationType = "WORK_ASSIGNMENT",
                TargetUserId = inboundOrder.AssignedToUserId.Value,
                ReferenceType = "INBOUND",
                ReferenceId = inboundOrder.InboundOrderId.ToString()
            }, currentUserId);
        }

        return inboundOrder.InboundOrderId;
    }

    public async Task<PurchaseOrderForInboundDto?> GetPurchaseOrderForInboundAsync(long purchaseOrderId)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseOrderDetails)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseOrderId);

        if (po == null) return null;
        if (NormalizePurchaseOrderStatus(po.Status) is not ("CONFIRMED" or "PARTIALLY_RECEIVED"))
            return null;
        if (po.Supplier == null || !IsActive(po.Supplier.Status))
            return null;

        var dto = new PurchaseOrderForInboundDto
        {
            PurchaseOrderId = po.PurchaseOrderId,
            PurchaseOrderNumber = po.PurchaseOrderNumber,
            SupplierName = po.Supplier?.SupplierName ?? string.Empty
        };

        // Find existing inbound quantities for this PO
        var inboundItems = await _context.InboundOrderItems
            .Include(i => i.InboundOrder)
            .Where(i => i.InboundOrder.PurchaseOrderId == purchaseOrderId && i.InboundOrder.Status != "CANCELLED")
            .ToListAsync();

        foreach (var detail in po.PurchaseOrderDetails)
        {
            var totalInbound = inboundItems
                .Where(i => i.ProductId == detail.ProductId)
                .Sum(i => i.ExpectedQuantity);

            var remaining = detail.OrderedQuantity - totalInbound;

            dto.Items.Add(new PurchaseOrderItemForInboundDto
            {
                ProductId = detail.ProductId,
                ProductCode = detail.Product.ProductCode,
                ProductName = detail.Product.ProductName,
                UnitName = detail.Product.UnitOfMeasure?.UnitName ?? string.Empty,
                QuantityScale = 0,
                TrackLot = detail.Product.TrackLot,
                TrackExpiry = detail.Product.TrackExpiry,
                OrderedQuantity = detail.OrderedQuantity,
                InboundQuantity = totalInbound,
                RemainingQuantity = remaining > 0 ? remaining : 0
            });
        }

        return dto;
    }

    public async Task<List<SourceOrderDropdownDto>> GetPendingPurchaseOrdersAsync()
    {
        var pos = await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.PurchaseOrderDetails)
            .Include(po => po.InboundOrders)
                .ThenInclude(io => io.InboundOrderItems)
            .Where(po => po.Status == "CONFIRMED" || po.Status == "PARTIALLY_RECEIVED" ||
                         po.Status == "PARTIALLYRECEIVED")
            .Where(po => po.Supplier != null &&
                         (po.Supplier.Status == "ACTIVE" || po.Supplier.Status == "AVAILABLE"))
            .ToListAsync();

        return pos
            .Where(po => 
            {
                var totalOrdered = po.PurchaseOrderDetails.Sum(d => d.OrderedQuantity);
                var totalExpected = po.InboundOrders
                    .Where(io => io.Status != "CANCELLED")
                    .SelectMany(io => io.InboundOrderItems)
                    .Sum(i => i.ExpectedQuantity);
                return totalOrdered > totalExpected;
            })
            .Select(po => new SourceOrderDropdownDto
            {
                Id = po.PurchaseOrderId,
                Name = $"{po.PurchaseOrderNumber} — {po.Supplier?.SupplierName ?? "N/A"}"
            })
            .ToList();
    }

    public async Task<List<SourceOrderDropdownDto>> GetReturnableSalesOrdersAsync()
    {
        var salesOrders = await _context.SalesOrders
            .Include(so => so.Customer)
            .Include(so => so.SalesOrderDetails)
            .Include(so => so.InboundOrders)
                .ThenInclude(io => io.InboundOrderItems)
            .Where(so => so.Status == "ISSUED"
                      || so.Status == "PARTIALLY_ISSUED"
                      || so.Status == "FULFILLED"
                      || so.Status == "PARTIALLY_FULFILLED")
            .Where(so => so.Customer.Status == "ACTIVE" || so.Customer.Status == "AVAILABLE")
            .ToListAsync();

        return salesOrders
            .Where(so => so.SalesOrderDetails.Any(detail =>
            {
                var alreadyPlannedForReturn = so.InboundOrders
                    .Where(io => io.Status != "CANCELLED")
                    .SelectMany(io => io.InboundOrderItems)
                    .Where(item => item.ProductId == detail.ProductId)
                    .Sum(item => item.ExpectedQuantity);

                return detail.FulfilledQuantity > alreadyPlannedForReturn;
            }))
            .Select(so => new SourceOrderDropdownDto
            {
                Id = so.SalesOrderId,
                Name = $"{so.SalesOrderNumber} — {so.Customer.CustomerName}"
            })
            .ToList();
    }

    public async Task<List<AvailableWarehouseStaffDto>> GetAvailableWarehouseStaffAsync()
    {
        return await _context.Users
            .Where(u => u.Status == "ACTIVE" && u.Role.RoleCode == "WAREHOUSE_STAFF" &&
                        !u.InboundOrderAssignedToUsers.Any(o =>
                            o.Status != "CANCELLED" && o.Status != "PUTAWAY_COMPLETED" &&
                            (o.Status != "COMPLETED" ||
                             o.InboundOrderItems.SelectMany(i => i.InboundOrderDetails)
                                 .Any(d => d.ConditionStatus == "GOOD" && d.InventoryTransaction == null))) &&
                        !u.OutboundOrderAssignedToUsers.Any(o =>
                            o.Status != "CANCELLED" && o.Status != "COMPLETED"))
            .OrderBy(u => u.FullName).ThenBy(u => u.Username)
            .Select(u => new AvailableWarehouseStaffDto
            {
                UserId = u.UserId,
                FullName = u.FullName
            })
            .ToListAsync();
    }

    public async Task<PurchaseOrderForInboundDto?> GetSalesOrderForInboundAsync(long salesOrderId)
    {
        var so = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.SalesOrderDetails)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.UnitOfMeasure)
            .Include(s => s.InboundOrders)
                .ThenInclude(io => io.InboundOrderItems)
            .FirstOrDefaultAsync(s => s.SalesOrderId == salesOrderId);

        if (so == null) return null;
        if (NormalizeSalesOrderStatus(so.Status) is not ("ISSUED" or "PARTIALLY_ISSUED"))
            return null;
        if (!IsActive(so.Customer.Status))
            return null;

        var dto = new PurchaseOrderForInboundDto
        {
            PurchaseOrderId = so.SalesOrderId,
            PurchaseOrderNumber = so.SalesOrderNumber,
            SupplierName = so.Customer.CustomerName,
            Items = so.SalesOrderDetails.Select(d => 
            {
                // Tương tự PO, tính số lượng đã tạo lệnh nhập kho trước đó cho SO này
                var totalExpectedBefore = so.InboundOrders
                    .Where(io => io.Status != "CANCELLED")
                    .SelectMany(io => io.InboundOrderItems)
                    .Where(ioItem => ioItem.ProductId == d.ProductId)
                    .Sum(ioItem => ioItem.ExpectedQuantity);

                // Sales Order có quantity trả lại tối đa bằng số lượng đã xuất kho (FulfilledQuantity)
                var remaining = d.FulfilledQuantity - totalExpectedBefore;
                
                return new PurchaseOrderItemForInboundDto
                {
                    ProductId = d.ProductId,
                    ProductCode = d.Product.ProductCode,
                    ProductName = d.Product.ProductName,
                    UnitName = d.Product.UnitOfMeasure?.UnitName ?? "",
                    QuantityScale = 0,
                    TrackLot = d.Product.TrackLot,
                    TrackExpiry = d.Product.TrackExpiry,
                    OrderedQuantity = d.FulfilledQuantity, // Gán tạm OrderedQuantity = FulfilledQuantity để frontend hiển thị đúng
                    RemainingQuantity = remaining > 0 ? remaining : 0
                };
            }).Where(i => i.RemainingQuantity > 0).ToList() // Chỉ lấy món còn hàng để nhập kho
        };

        return dto;
    }

    private static string NormalizePurchaseOrderStatus(string? status)
    {
        return (status ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "PARTIALLYRECEIVED" => "PARTIALLY_RECEIVED",
            "COMPLETED" or "CLOSED" => "RECEIVED",
            var value => value
        };
    }

    private static string NormalizeSalesOrderStatus(string? status)
    {
        return (status ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "PARTIALLY_FULFILLED" => "PARTIALLY_ISSUED",
            "FULFILLED" or "CLOSED" => "ISSUED",
            var value => value
        };
    }

    public async Task UpdateInboundOrderAsync(long id, UpdateInboundOrderDto dto, long currentUserId)
    {
        await using var assignmentTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var order = await _context.InboundOrders
            .Include(o => o.InboundOrderItems)
                .ThenInclude(i => i.InboundOrderDetails)
            .FirstOrDefaultAsync(o => o.InboundOrderId == id);

        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho");
        if (order.Status is not ("DRAFT" or "ASSIGNED") || order.InboundOrderItems.Any(i => i.InboundOrderDetails.Count > 0))
            throw new Exception("Chỉ có thể sửa phiếu ở trạng thái Nháp hoặc Sẵn sàng, trước khi bắt đầu kiểm nhận.");

        if (dto.Items == null || !dto.Items.Any())
            throw new Exception("Lệnh nhập kho phải có ít nhất 1 dòng hàng.");

        if (dto.Items.Any(i => i.ExpectedQuantity <= 0))
            throw new Exception("Số lượng dự kiến phải lớn hơn 0.");
        if (dto.Items.GroupBy(i => i.ProductId).Any(group => group.Count() > 1))
            throw new Exception("Mỗi sản phẩm chỉ được xuất hiện một lần trong phiếu nhập kho.");
        if ((dto.IsSubmit || order.Status == "ASSIGNED") && !dto.AssignedToUserId.HasValue)
            throw new Exception("Vui lòng phân công nhân viên kho trước khi chuyển phiếu sang trạng thái Sẵn sàng.");

        await EnsureWarehouseStaffAssigneeAsync(dto.AssignedToUserId, excludeInboundOrderId: order.InboundOrderId);
        await ValidateQuantitiesAsync(dto.Items.Select(i => (i.ProductId, i.ExpectedQuantity, "Số lượng dự kiến")));

        if (order.SourceType == "PURCHASE_ORDER" && order.PurchaseOrderId.HasValue)
        {
            var po = await GetPurchaseOrderForInboundAsync(order.PurchaseOrderId.Value);
            foreach (var item in dto.Items)
            {
                var poItem = po?.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                var currentQuantity = order.InboundOrderItems.FirstOrDefault(i => i.ProductId == item.ProductId)?.ExpectedQuantity ?? 0;
                var eligibleQuantity = (poItem?.RemainingQuantity ?? 0) + currentQuantity;
                if (poItem == null || item.ExpectedQuantity > eligibleQuantity)
                    throw new Exception($"Số lượng dự kiến của sản phẩm {poItem?.ProductName ?? item.ProductId.ToString()} vượt quá số lượng còn đủ điều kiện ({eligibleQuantity}).");
            }
        }
        else if (order.SourceType == "SALES_RETURN" && order.SalesOrderId.HasValue)
        {
            var salesOrder = await GetSalesOrderForInboundAsync(order.SalesOrderId.Value);
            foreach (var item in dto.Items)
            {
                var salesItem = salesOrder?.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                var currentQuantity = order.InboundOrderItems.FirstOrDefault(i => i.ProductId == item.ProductId)?.ExpectedQuantity ?? 0;
                var eligibleQuantity = (salesItem?.RemainingQuantity ?? 0) + currentQuantity;
                if (salesItem == null || item.ExpectedQuantity > eligibleQuantity)
                    throw new Exception($"Số lượng khách trả của sản phẩm {salesItem?.ProductName ?? item.ProductId.ToString()} vượt quá số đã xuất còn đủ điều kiện ({eligibleQuantity}).");
            }
        }

        order.ExpectedReceiptDate = dto.ExpectedReceiptDate;
        order.Notes = dto.Notes;

        var previousAssigneeId = order.AssignedToUserId;
        var submittedNow = order.Status == "DRAFT" && dto.IsSubmit;
        if (submittedNow)
        {
            order.Status = "ASSIGNED";
            order.ConfirmedAt = DateTime.UtcNow;
            order.ConfirmedByUserId = currentUserId;
        }
        order.AssignedToUserId = dto.AssignedToUserId;

        _context.InboundOrderItems.RemoveRange(order.InboundOrderItems);
        order.InboundOrderItems.Clear();
        foreach (var itemDto in dto.Items)
        {
            order.InboundOrderItems.Add(new BMWMS.Repository.Models.InboundOrderItem
            {
                ProductId = itemDto.ProductId,
                ExpectedQuantity = itemDto.ExpectedQuantity,
                ReceivedQuantity = 0,
                DamagedQuantity = 0,
                ShortageQuantity = 0,
                Notes = itemDto.Notes
            });
        }

        await _context.SaveChangesAsync();
        await assignmentTransaction.CommitAsync();

        if (order.Status == "ASSIGNED" && order.AssignedToUserId.HasValue &&
            (submittedNow || previousAssigneeId != order.AssignedToUserId))
        {
            await _notificationService.CreateNotificationAsync(new BMWMS.Business.DTOs.Notification.CreateNotificationDto
            {
                Title = "Bạn được giao một lệnh nhập kho mới",
                Message = $"Lệnh nhập kho {order.InboundOrderNumber} đã được giao cho bạn. Vui lòng kiểm tra.",
                NotificationType = "WORK_ASSIGNMENT",
                TargetUserId = order.AssignedToUserId.Value,
                ReferenceType = "INBOUND",
                ReferenceId = order.InboundOrderId.ToString()
            }, currentUserId);
        }
    }

    public async Task CancelInboundOrderAsync(long id, CancelInboundOrderDto dto, long currentUserId)
    {
        if (string.IsNullOrWhiteSpace(dto.CancellationReason))
            throw new ArgumentException("Vui lòng nhập lý do hủy phiếu.");

        var order = await _context.InboundOrders
            .Include(o => o.InboundOrderItems)
                .ThenInclude(i => i.InboundOrderDetails)
            .FirstOrDefaultAsync(o => o.InboundOrderId == id);
        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho");
        if (order.Status is not ("DRAFT" or "ASSIGNED") || order.InboundOrderItems.Any(i => i.InboundOrderDetails.Count > 0))
            throw new Exception("Chỉ có thể hủy phiếu ở trạng thái Nháp hoặc Sẵn sàng, trước khi bắt đầu kiểm nhận.");

        order.Status = "CANCELLED";
        order.CancellationReason = dto.CancellationReason.Trim();
        order.CancelledAt = DateTime.UtcNow;
        order.CancelledByUserId = currentUserId;

        await _context.SaveChangesAsync();
    }

    public async Task ConfirmInboundOrderAsync(long id, long currentUserId)
    {
        var order = await _inboundRepository.GetByIdAsync(id);
        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho");
        if (order.Status != "DRAFT") throw new Exception("Chỉ có thể xác nhận lệnh nhập kho ở trạng thái Chờ xử lý (DRAFT)");

        await EnsureWarehouseStaffAssigneeAsync(order.AssignedToUserId, excludeInboundOrderId: order.InboundOrderId);
        if (!order.AssignedToUserId.HasValue)
            throw new InvalidOperationException("Phải phân công một nhân viên kho trước khi chuyển phiếu sang trạng thái Sẵn sàng.");

        order.Status = "ASSIGNED";
        order.ConfirmedAt = DateTime.UtcNow;
        order.ConfirmedByUserId = currentUserId;

        await _inboundRepository.UpdateAsync(order);
        await _inboundRepository.SaveChangesAsync();

        await _notificationService.CreateNotificationAsync(new BMWMS.Business.DTOs.Notification.CreateNotificationDto
        {
            Title = "Bạn được giao một phiếu nhập kho",
            Message = $"Phiếu nhập kho {order.InboundOrderNumber} đã sẵn sàng để kiểm nhận.",
            NotificationType = "WORK_ASSIGNMENT",
            TargetUserId = order.AssignedToUserId.Value,
            ReferenceType = "INBOUND",
            ReferenceId = order.InboundOrderId.ToString()
        }, currentUserId);
    }

    private async Task<long> ReceiveItemLegacyAsync(long inboundOrderId, ReceiveInboundItemDto dto, long currentUserId)
    {
        var order = await _context.InboundOrders
            .Include(o => o.InboundOrderItems)
            .FirstOrDefaultAsync(o => o.InboundOrderId == inboundOrderId);

        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho.");

        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == currentUserId);
        if (user?.Role?.RoleCode == "WAREHOUSE_STAFF" && order.AssignedToUserId != currentUserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền nhận hàng cho lệnh này vì nó không được phân công cho bạn.");
        }
        if (order.Status != "ASSIGNED" && order.Status != "IN_PROGRESS") 
            throw new Exception("Chỉ có thể nhận hàng khi lệnh ở trạng thái ASSIGNED hoặc IN_PROGRESS.");

        var item = order.InboundOrderItems.FirstOrDefault(i => i.InboundOrderItemId == dto.InboundOrderItemId);
        if (item == null) throw new Exception("Không tìm thấy sản phẩm trong lệnh nhập kho.");

        // Calculate Accepted Quantity
        decimal acceptedQuantity = dto.DeliveredQuantity - dto.DamagedQuantity;
        if (acceptedQuantity <= 0) throw new Exception("Số lượng chấp nhận phải lớn hơn 0.");

        // Update item quantities
        item.ReceivedQuantity += acceptedQuantity;
        item.DamagedQuantity += dto.DamagedQuantity;

        // Create or find ProductLot
        var legacyLotNumber = string.IsNullOrWhiteSpace(dto.LotNumber) ? $"NO-LOT-{item.ProductId}" : dto.LotNumber.Trim();
        var lot = await _context.ProductLots
            .FirstOrDefaultAsync(l => l.ProductId == item.ProductId && l.LotNumber == legacyLotNumber);

        if (lot == null)
        {
            lot = new BMWMS.Repository.Models.ProductLot
            {
                ProductId = item.ProductId,
                LotNumber = legacyLotNumber,
                ExpiryDate = dto.ExpiryDate,
                FirstReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "AVAILABLE",
                CreatedAt = DateTime.UtcNow
            };
            _context.ProductLots.Add(lot);
            await _context.SaveChangesAsync(); // Save to get ProductLotId
        }

        // Find Staging Location (Assuming Warehouse has a default or staging location)
        // For simplicity, we find any location in this warehouse. In a real system, you'd filter by LocationType == "RECEIVING"
        var stagingLocation = await _context.StorageLocations
            .FirstOrDefaultAsync(l => l.WarehouseId == order.WarehouseId);

        if (stagingLocation == null) throw new Exception("Không tìm thấy vị trí lưu trữ nào trong kho này để nhận tạm.");

        // Create InboundOrderDetail for receipt
        var detail = new BMWMS.Repository.Models.InboundOrderDetail
        {
            InboundOrderItemId = item.InboundOrderItemId,
            InboundOrderId = inboundOrderId,
            ProductId = item.ProductId,
            StorageLocationId = stagingLocation.StorageLocationId,
            ProductLotId = lot.ProductLotId,
            ReceivedQuantity = acceptedQuantity,
            ConditionStatus = "GOOD",
            RecordedByUserId = currentUserId,
            RecordedAt = DateTime.UtcNow
        };
        _context.InboundOrderDetails.Add(detail);
        await _context.SaveChangesAsync(); // To get ID

        // Create InventoryTransaction for receipt
        var transaction = new BMWMS.Repository.Models.InventoryTransaction
        {
            TransactionType = "RECEIPT",
            ProductId = item.ProductId,
            StorageLocationId = stagingLocation.StorageLocationId,
            ProductLotId = lot.ProductLotId,
            OnHandDelta = acceptedQuantity,
            ReservedDelta = 0,
            InboundOrderDetailId = detail.InboundOrderDetailId,
            TransactionAt = DateTime.UtcNow,
            PerformedByUserId = currentUserId
        };
        _context.InventoryTransactions.Add(transaction);

        // Update Order Status
        if (order.Status == "ASSIGNED")
        {
            order.Status = "IN_PROGRESS";
        }

        // Check if fully received
        if (order.InboundOrderItems.All(i => i.ReceivedQuantity >= i.ExpectedQuantity))
        {
            order.Status = "COMPLETED";
        }

        await UpdatePurchaseOrderReceiptStatusAsync(order.PurchaseOrderId);
        await _context.SaveChangesAsync();
        return lot.ProductLotId;
    }

    private async Task ReceiveBatchLegacyAsync(long inboundOrderId, ReceiveBatchInboundDto dto, long currentUserId)
    {
        var order = await _context.InboundOrders
            .Include(o => o.InboundOrderItems)
            .FirstOrDefaultAsync(o => o.InboundOrderId == inboundOrderId);

        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho.");

        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == currentUserId);
        if (user?.Role?.RoleCode == "WAREHOUSE_STAFF" && order.AssignedToUserId != currentUserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền nhận hàng cho lệnh này vì nó không được phân công cho bạn.");
        }
        if (order.Status != "ASSIGNED" && order.Status != "IN_PROGRESS") 
            throw new Exception("Chỉ có thể nhận hàng khi lệnh ở trạng thái ASSIGNED hoặc IN_PROGRESS.");

        var stagingLocation = await _context.StorageLocations
            .FirstOrDefaultAsync(l => l.WarehouseId == order.WarehouseId);

        if (stagingLocation == null) throw new Exception("Không tìm thấy vị trí lưu trữ nào trong kho này để nhận tạm.");

        foreach (var reqItem in dto.Items)
        {
            var item = order.InboundOrderItems.FirstOrDefault(i => i.InboundOrderItemId == reqItem.InboundOrderItemId);
            if (item == null) continue;

            decimal acceptedQuantity = reqItem.DeliveredQuantity - reqItem.DamagedQuantity;
            if (acceptedQuantity <= 0) continue;

            item.ReceivedQuantity += acceptedQuantity;
            item.DamagedQuantity += reqItem.DamagedQuantity;

            var legacyLotNumber = string.IsNullOrWhiteSpace(reqItem.LotNumber) ? $"NO-LOT-{item.ProductId}" : reqItem.LotNumber.Trim();
            var lot = await _context.ProductLots
                .FirstOrDefaultAsync(l => l.ProductId == item.ProductId && l.LotNumber == legacyLotNumber);

            if (lot == null)
            {
                lot = new BMWMS.Repository.Models.ProductLot
                {
                    ProductId = item.ProductId,
                    LotNumber = legacyLotNumber,
                    ExpiryDate = reqItem.ExpiryDate,
                    FirstReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Status = "AVAILABLE",
                    CreatedAt = DateTime.UtcNow
                };
                _context.ProductLots.Add(lot);
                await _context.SaveChangesAsync();
            }

            var detail = new BMWMS.Repository.Models.InboundOrderDetail
            {
                InboundOrderItemId = item.InboundOrderItemId,
                InboundOrderId = inboundOrderId,
                ProductId = item.ProductId,
                StorageLocationId = stagingLocation.StorageLocationId,
                ProductLotId = lot.ProductLotId,
                ReceivedQuantity = acceptedQuantity,
                ConditionStatus = "GOOD",
                RecordedByUserId = currentUserId,
                RecordedAt = DateTime.UtcNow
            };
            _context.InboundOrderDetails.Add(detail);
            await _context.SaveChangesAsync();

            var transaction = new BMWMS.Repository.Models.InventoryTransaction
            {
                TransactionType = "RECEIPT",
                ProductId = item.ProductId,
                StorageLocationId = stagingLocation.StorageLocationId,
                ProductLotId = lot.ProductLotId,
                OnHandDelta = acceptedQuantity,
                ReservedDelta = 0,
                InboundOrderDetailId = detail.InboundOrderDetailId,
                TransactionAt = DateTime.UtcNow,
                PerformedByUserId = currentUserId
            };
            _context.InventoryTransactions.Add(transaction);
        }

        if (order.Status == "ASSIGNED")
        {
            order.Status = "IN_PROGRESS";
        }

        // Check if fully received
        if (order.InboundOrderItems.All(i => i.ReceivedQuantity >= i.ExpectedQuantity))
        {
            order.Status = "COMPLETED";
        }

        await UpdatePurchaseOrderReceiptStatusAsync(order.PurchaseOrderId);
        await _context.SaveChangesAsync();
    }

    private async Task UpdatePurchaseOrderReceiptStatusAsync(long? purchaseOrderId)
    {
        if (!purchaseOrderId.HasValue)
            return;

        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.PurchaseOrderDetails)
            .Include(po => po.InboundOrders)
                .ThenInclude(io => io.InboundOrderItems)
                    .ThenInclude(item => item.InboundOrderDetails)
            .FirstOrDefaultAsync(po => po.PurchaseOrderId == purchaseOrderId.Value);

        if (purchaseOrder == null || NormalizePurchaseOrderStatus(purchaseOrder.Status) == "CANCELLED")
            return;

        var activeInboundItems = purchaseOrder.InboundOrders
            .Where(io => io.Status == "COMPLETED")
            .SelectMany(io => io.InboundOrderItems)
            .ToList();

        var hasReceivedQuantity = activeInboundItems.Any(item =>
            item.InboundOrderDetails.Any(receipt => receipt.ConditionStatus == "GOOD" && receipt.ReceivedQuantity > 0));
        var isFullyReceived = purchaseOrder.PurchaseOrderDetails.Count > 0
            && purchaseOrder.PurchaseOrderDetails.All(detail =>
                activeInboundItems
                    .Where(item => item.ProductId == detail.ProductId)
                    .SelectMany(item => item.InboundOrderDetails)
                    .Where(receipt => receipt.ConditionStatus == "GOOD")
                    .Sum(receipt => receipt.ReceivedQuantity) >= detail.OrderedQuantity);

        purchaseOrder.Status = isFullyReceived
            ? "RECEIVED"
            : hasReceivedQuantity
                ? "PARTIALLY_RECEIVED"
                : "CONFIRMED";
        purchaseOrder.UpdatedAt = DateTime.UtcNow;
    }

    private async Task PutawayBatchLegacyAsync(long inboundOrderId, List<PutawayInboundItemDto> dtos, long currentUserId)
    {
        var order = await _context.InboundOrders
            .Include(o => o.InboundOrderItems)
            .FirstOrDefaultAsync(o => o.InboundOrderId == inboundOrderId);

        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho.");

        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == currentUserId);
        if (user?.Role?.RoleCode == "WAREHOUSE_STAFF" && order.AssignedToUserId != currentUserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xếp vị trí cho lệnh này vì nó không được phân công cho bạn.");
        }

        decimal totalPutawayCurrentBatch = 0;

        foreach (var dto in dtos)
        {
            var item = order.InboundOrderItems.FirstOrDefault(i => i.InboundOrderItemId == dto.InboundOrderItemId);
            if (item == null) continue;

            var transaction = new BMWMS.Repository.Models.InventoryTransaction
            {
                TransactionType = "PUTAWAY",
                ProductId = item.ProductId,
                StorageLocationId = dto.StorageLocationId,
                ProductLotId = dto.ProductLotId,
                OnHandDelta = dto.PutawayQuantity,
                ReservedDelta = 0,
                InboundOrderDetailId = null,
                TransactionAt = DateTime.UtcNow,
                PerformedByUserId = currentUserId
            };
            _context.InventoryTransactions.Add(transaction);

            // Cập nhật tồn kho (Inventory)
            var inventory = await _context.Inventories.FirstOrDefaultAsync(inv => 
                inv.ProductId == item.ProductId && 
                inv.StorageLocationId == dto.StorageLocationId && 
                inv.ProductLotId == dto.ProductLotId);
            
            if (inventory == null)
            {
                inventory = new BMWMS.Repository.Models.Inventory
                {
                    ProductId = item.ProductId,
                    StorageLocationId = dto.StorageLocationId,
                    ProductLotId = dto.ProductLotId,
                    OnHandQuantity = dto.PutawayQuantity,
                    AvailableQuantity = dto.PutawayQuantity,
                    ReservedQuantity = 0,
                    LastUpdatedAt = DateTime.UtcNow
                };
                _context.Inventories.Add(inventory);
            }
            else
            {
                inventory.OnHandQuantity += dto.PutawayQuantity;
                inventory.AvailableQuantity += dto.PutawayQuantity;
                inventory.LastUpdatedAt = DateTime.UtcNow;
            }

            totalPutawayCurrentBatch += dto.PutawayQuantity;
        }

        var totalPutaway = await _context.InventoryTransactions
            .Where(t => t.TransactionType == "PUTAWAY" && _context.InboundOrderItems.Where(i => i.InboundOrderId == inboundOrderId).Select(i => i.ProductId).Contains(t.ProductId))
            .SumAsync(t => t.OnHandDelta);

        var totalExpected = order.InboundOrderItems.Sum(i => i.ExpectedQuantity);

        if (totalPutaway + totalPutawayCurrentBatch >= totalExpected)
        {
            order.Status = "PUTAWAY_COMPLETED";
        }

        await _context.SaveChangesAsync();
    }

    public async Task<long> ReceiveItemAsync(long inboundOrderId, ReceiveInboundItemDto dto, long currentUserId)
    {
        var lotIds = await ReceiveBatchCoreAsync(inboundOrderId, new[] { dto }, currentUserId);
        return lotIds.Single();
    }

    public async Task ReceiveBatchAsync(long inboundOrderId, ReceiveBatchInboundDto dto, long currentUserId)
    {
        await ReceiveBatchCoreAsync(inboundOrderId, dto.Items, currentUserId);
    }

    public async Task CompleteReceiptAsync(long inboundOrderId, CompleteInboundReceiptDto dto, long currentUserId)
    {
        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var order = await _context.InboundOrders
                .Include(o => o.InboundOrderItems)
                    .ThenInclude(i => i.InboundOrderDetails)
                .Include(o => o.InboundOrderItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.InboundOrderId == inboundOrderId)
                ?? throw new InvalidOperationException("Không tìm thấy phiếu nhập kho.");

            await EnsureAssignedUserAsync(order.AssignedToUserId, currentUserId, "hoàn tất kiểm nhận");
            if (order.Status is not ("ASSIGNED" or "IN_PROGRESS"))
                throw new InvalidOperationException("Chỉ được hoàn tất kiểm nhận khi phiếu ở trạng thái Sẵn sàng hoặc Đang nhận.");

            var confirmedShortageItemIds = (dto.ConfirmedShortageItemIds ?? new List<long>())
                .Distinct()
                .ToHashSet();
            var orderItemIds = order.InboundOrderItems.Select(item => item.InboundOrderItemId).ToHashSet();
            if (confirmedShortageItemIds.Any(itemId => !orderItemIds.Contains(itemId)))
                throw new ArgumentException("Danh sách xác nhận hàng thiếu chứa dòng không thuộc phiếu nhập kho.");

            var unconfirmedItems = order.InboundOrderItems
                .Where(item => item.ReceivedQuantity < item.ExpectedQuantity &&
                               !confirmedShortageItemIds.Contains(item.InboundOrderItemId))
                .ToList();
            if (unconfirmedItems.Count > 0)
            {
                var productList = string.Join(", ", unconfirmedItems.Select(item =>
                    $"{item.Product.ProductName} ({item.Product.ProductCode})"));
                throw new InvalidOperationException(
                    $"Chưa kiểm nhận đủ hoặc chưa xác nhận số lượng thiếu cho: {productList}.");
            }

            foreach (var item in order.InboundOrderItems)
            {
                var acceptedQuantity = item.InboundOrderDetails
                    .Where(d => d.ConditionStatus == "GOOD")
                    .Sum(d => d.ReceivedQuantity);
                item.ShortageQuantity = Math.Max(0, item.ExpectedQuantity - acceptedQuantity);
            }

            order.Status = "COMPLETED";
            await UpdatePurchaseOrderReceiptStatusAsync(order.PurchaseOrderId);
            await _context.SaveChangesAsync();

            var targetUsers = await _context.Users
                .Where(u => u.Status == "ACTIVE" &&
                            (u.Role.RoleCode == "PURCHASING_STAFF" || u.Role.RoleCode == "WAREHOUSE_MANAGER") &&
                            u.UserId != currentUserId)
                .Select(u => u.UserId)
                .ToListAsync();
            foreach (var targetUserId in targetUsers)
            {
                await _notificationService.CreateNotificationAsync(new BMWMS.Business.DTOs.Notification.CreateNotificationDto
                {
                    Title = "Phiếu nhập đã hoàn tất kiểm nhận",
                    Message = $"Phiếu {order.InboundOrderNumber} đã chốt số lượng thực nhận, thiếu và hỏng; hàng đạt đang chờ xếp vị trí.",
                    NotificationType = "SYSTEM",
                    TargetUserId = targetUserId,
                    ReferenceType = "INBOUND",
                    ReferenceId = order.InboundOrderId.ToString()
                }, currentUserId);
            }
            await dbTransaction.CommitAsync();
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    private async Task<List<long>> ReceiveBatchCoreAsync(
        long inboundOrderId,
        IEnumerable<ReceiveInboundItemDto> requests,
        long currentUserId)
    {
        var requestItems = requests.Where(x => x.DeliveredQuantity > 0).ToList();
        if (requestItems.Count == 0)
            throw new ArgumentException("Phải có ít nhất một dòng hàng có số lượng thực giao lớn hơn 0.");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var order = await _context.InboundOrders
                .Include(o => o.InboundOrderItems).ThenInclude(i => i.Product).ThenInclude(p => p.UnitOfMeasure)
                .FirstOrDefaultAsync(o => o.InboundOrderId == inboundOrderId)
                ?? throw new InvalidOperationException("Không tìm thấy phiếu nhập kho.");

            await EnsureAssignedUserAsync(order.AssignedToUserId, currentUserId, "nhận hàng");
            if (order.Status is not ("ASSIGNED" or "IN_PROGRESS"))
                throw new InvalidOperationException("Chỉ được nhận hàng khi phiếu ở trạng thái Sẵn sàng hoặc Đang nhận.");

            var receiving = await _context.StorageLocations.FirstOrDefaultAsync(l =>
                l.WarehouseId == order.WarehouseId && l.LocationType == "RECEIVING" &&
                l.Status != "BLOCKED" && l.Status != "INACTIVE")
                ?? await _context.StorageLocations.FirstOrDefaultAsync(l =>
                    l.WarehouseId == order.WarehouseId && l.LocationType == "STAGING" &&
                    l.Status != "BLOCKED" && l.Status != "INACTIVE")
                ?? throw new InvalidOperationException("Kho chưa cấu hình vị trí RECEIVING/STAGING đang hoạt động.");

            var hasNonAccepted = requestItems.Any(x =>
                x.DamagedQuantity > 0 ||
                (x.AcceptedQuantity.HasValue && x.AcceptedQuantity.Value + x.DamagedQuantity < x.DeliveredQuantity));
            var quarantine = hasNonAccepted
                ? await _context.StorageLocations.FirstOrDefaultAsync(l =>
                    l.WarehouseId == order.WarehouseId && l.LocationType == "QUARANTINE" &&
                    l.Status != "BLOCKED" && l.Status != "INACTIVE")
                : null;
            if (hasNonAccepted && quarantine == null)
                throw new InvalidOperationException("Kho chưa cấu hình vị trí QUARANTINE để cách ly hàng hỏng hoặc hàng chưa được chấp nhận.");

            var lotIds = new List<long>();
            foreach (var request in requestItems)
            {
                var accepted = request.AcceptedQuantity ?? (request.DeliveredQuantity - request.DamagedQuantity);
                if (accepted < 0 || request.DamagedQuantity < 0 || accepted + request.DamagedQuantity > request.DeliveredQuantity)
                    throw new ArgumentException("Số lượng đạt và số lượng hỏng phải không âm; tổng hai loại không được vượt số lượng thực giao.");
                var quarantined = request.DeliveredQuantity - accepted - request.DamagedQuantity;

                var item = order.InboundOrderItems.SingleOrDefault(i => i.InboundOrderItemId == request.InboundOrderItemId)
                    ?? throw new ArgumentException("Dòng hàng không thuộc phiếu nhập kho.");
                if (!IsActive(item.Product.Status))
                    throw new InvalidOperationException($"Sản phẩm {item.Product.ProductCode} đang ngừng hoạt động.");
                QuantityRules.EnsureValid(item.Product, request.DeliveredQuantity, "Số lượng thực giao");
                if (accepted > 0) QuantityRules.EnsureValid(item.Product, accepted, "Số lượng đạt");
                if (request.DamagedQuantity > 0) QuantityRules.EnsureValid(item.Product, request.DamagedQuantity, "Số lượng hỏng");
                if (quarantined > 0) QuantityRules.EnsureValid(item.Product, quarantined, "Số lượng cách ly");
                if (item.ReceivedQuantity + request.DeliveredQuantity > item.ExpectedQuantity)
                    throw new ArgumentException($"Số lượng nhận của {item.Product.ProductCode} vượt số lượng dự kiến còn lại.");
                if (item.Product.TrackLot && string.IsNullOrWhiteSpace(request.LotNumber))
                    throw new ArgumentException($"Phải nhập số lô cho {item.Product.ProductCode}.");
                if (item.Product.TrackExpiry &&
                    (!request.ExpiryDate.HasValue || request.ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow)))
                    throw new ArgumentException($"Sản phẩm {item.Product.ProductCode} phải có hạn dùng trong tương lai.");
                if (request.ManufactureDate.HasValue && request.ManufactureDate.Value > DateOnly.FromDateTime(DateTime.UtcNow))
                    throw new ArgumentException($"Ngày sản xuất của {item.Product.ProductCode} không được nằm trong tương lai.");
                if (request.ManufactureDate.HasValue && request.ExpiryDate.HasValue && request.ManufactureDate > request.ExpiryDate)
                    throw new ArgumentException($"Ngày sản xuất của {item.Product.ProductCode} không được sau hạn dùng.");

                var lotNumber = item.Product.TrackLot ? request.LotNumber!.Trim() : $"NO-LOT-{item.ProductId}";
                var expiryDate = item.Product.TrackExpiry ? request.ExpiryDate : null;
                var lot = await _context.ProductLots.FirstOrDefaultAsync(l =>
                    l.ProductId == item.ProductId && l.LotNumber == lotNumber);
                if (lot == null)
                {
                    lot = new BMWMS.Repository.Models.ProductLot
                    {
                        ProductId = item.ProductId,
                        LotNumber = lotNumber,
                        ManufactureDate = request.ManufactureDate,
                        ExpiryDate = expiryDate,
                        FirstReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        Status = "AVAILABLE",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ProductLots.Add(lot);
                    await _context.SaveChangesAsync();
                }
                else if (lot.ExpiryDate != expiryDate ||
                         (request.ManufactureDate.HasValue && lot.ManufactureDate != request.ManufactureDate))
                {
                    throw new ArgumentException($"Ngày sản xuất hoặc hạn dùng của lô {lotNumber} không khớp dữ liệu đã có.");
                }

                var previouslyAccepted = await _context.InboundOrderDetails
                    .Where(d => d.InboundOrderItemId == item.InboundOrderItemId && d.ConditionStatus == "GOOD")
                    .SumAsync(d => (decimal?)d.ReceivedQuantity) ?? 0;
                item.ReceivedQuantity += request.DeliveredQuantity;
                item.DamagedQuantity += request.DamagedQuantity;
                item.ShortageQuantity = Math.Max(0, item.ExpectedQuantity - previouslyAccepted - accepted);

                if (accepted > 0)
                {
                    _context.InboundOrderDetails.Add(new BMWMS.Repository.Models.InboundOrderDetail
                    {
                        InboundOrderItemId = item.InboundOrderItemId,
                        InboundOrderId = order.InboundOrderId,
                        ProductId = item.ProductId,
                        StorageLocationId = receiving.StorageLocationId,
                        ProductLotId = lot.ProductLotId,
                        ReceivedQuantity = accepted,
                        ConditionStatus = "GOOD",
                        RecordedByUserId = currentUserId,
                        RecordedAt = DateTime.UtcNow,
                        Notes = string.IsNullOrWhiteSpace(request.ConditionNotes)
                            ? "Hàng đạt; chờ xếp vào vị trí cố định."
                            : request.ConditionNotes.Trim()
                    });
                }

                if (request.DamagedQuantity > 0)
                {
                    _context.InboundOrderDetails.Add(new BMWMS.Repository.Models.InboundOrderDetail
                    {
                        InboundOrderItemId = item.InboundOrderItemId,
                        InboundOrderId = order.InboundOrderId,
                        ProductId = item.ProductId,
                        StorageLocationId = quarantine!.StorageLocationId,
                        ProductLotId = lot.ProductLotId,
                        ReceivedQuantity = request.DamagedQuantity,
                        ConditionStatus = "DAMAGED",
                        RecordedByUserId = currentUserId,
                        RecordedAt = DateTime.UtcNow,
                        Notes = string.IsNullOrWhiteSpace(request.ConditionNotes)
                            ? "Hàng hỏng được cách ly; chưa cộng vào tồn khả dụng."
                            : request.ConditionNotes.Trim()
                    });
                }

                if (quarantined > 0)
                {
                    _context.InboundOrderDetails.Add(new BMWMS.Repository.Models.InboundOrderDetail
                    {
                        InboundOrderItemId = item.InboundOrderItemId,
                        InboundOrderId = order.InboundOrderId,
                        ProductId = item.ProductId,
                        StorageLocationId = quarantine!.StorageLocationId,
                        ProductLotId = lot.ProductLotId,
                        ReceivedQuantity = quarantined,
                        ConditionStatus = "QUARANTINED",
                        RecordedByUserId = currentUserId,
                        RecordedAt = DateTime.UtcNow,
                        Notes = string.IsNullOrWhiteSpace(request.ConditionNotes)
                            ? "Hàng chưa được chấp nhận, tạm cách ly để xử lý."
                            : request.ConditionNotes.Trim()
                    });
                }

                lotIds.Add(lot.ProductLotId);
            }

            order.Status = "IN_PROGRESS";

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            return lotIds;
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    public async Task PutawayBatchAsync(long inboundOrderId, List<PutawayInboundItemDto> dtos, long currentUserId)
    {
        if (dtos == null || dtos.Count == 0 || dtos.Any(x => x.PutawayQuantity <= 0))
            throw new ArgumentException("Danh sách xếp hàng vào vị trí không hợp lệ.");
        if (dtos.GroupBy(x => new { x.InboundOrderItemId, x.ProductLotId, x.StorageLocationId })
            .Any(group => group.Count() > 1))
            throw new ArgumentException("Không được lặp cùng một sản phẩm, lô và vị trí đích trong một lần phân bổ.");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var order = await _context.InboundOrders
                .Include(o => o.InboundOrderItems)
                    .ThenInclude(i => i.InboundOrderDetails)
                        .ThenInclude(d => d.InventoryTransaction)
                .Include(o => o.InboundOrderItems)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.UnitOfMeasure)
                .FirstOrDefaultAsync(o => o.InboundOrderId == inboundOrderId)
                ?? throw new InvalidOperationException("Không tìm thấy phiếu nhập kho.");

            await EnsureAssignedUserAsync(order.AssignedToUserId, currentUserId, "xếp hàng vào vị trí");
            if (order.Status != "COMPLETED")
                throw new InvalidOperationException("Chỉ được xếp hàng vào vị trí sau khi đã hoàn tất kiểm nhận.");

            foreach (var allocation in dtos)
            {
                var item = order.InboundOrderItems.SingleOrDefault(i => i.InboundOrderItemId == allocation.InboundOrderItemId)
                    ?? throw new ArgumentException("Dòng hàng không thuộc phiếu nhập kho.");
                QuantityRules.EnsureValid(item.Product, allocation.PutawayQuantity, "Số lượng xếp vị trí");
                var destination = await _context.StorageLocations
                    .Include(location => location.StorageRack)
                        .ThenInclude(rack => rack!.WarehouseZone)
                    .FirstOrDefaultAsync(l => l.StorageLocationId == allocation.StorageLocationId)
                    ?? throw new ArgumentException("Không tìm thấy vị trí đích.");

                if (destination.WarehouseId != order.WarehouseId || destination.LocationType != "BIN" ||
                    !destination.IsPutawayAllowed || !IsActive(destination.Status))
                    throw new InvalidOperationException($"Vị trí {destination.LocationCode} không hợp lệ để xếp hàng.");
                if (destination.StorageRack != null &&
                    (!IsActive(destination.StorageRack.Status) ||
                     !IsActive(destination.StorageRack.WarehouseZone.Status)))
                    throw new InvalidOperationException($"Khu hoặc kệ chứa vị trí {destination.LocationCode} đang bị khóa/ngừng hoạt động.");

                var remaining = allocation.PutawayQuantity;
                var stagingDetails = item.InboundOrderDetails
                    .Where(d => d.ProductLotId == allocation.ProductLotId && d.ConditionStatus == "GOOD" &&
                                d.InventoryTransaction == null)
                    .OrderBy(d => d.RecordedAt)
                    .ToList();
                if (stagingDetails.Sum(d => d.ReceivedQuantity) < remaining)
                    throw new InvalidOperationException("Số lượng phân bổ vượt số hàng đạt đang chờ xếp vị trí.");

                foreach (var staging in stagingDetails.Where(_ => remaining > 0))
                {
                    var moved = Math.Min(staging.ReceivedQuantity, remaining);
                    BMWMS.Repository.Models.InboundOrderDetail postedDetail;
                    if (moved == staging.ReceivedQuantity)
                    {
                        staging.StorageLocationId = destination.StorageLocationId;
                        staging.Notes = "Đã xếp vào vị trí cố định.";
                        postedDetail = staging;
                    }
                    else
                    {
                        staging.ReceivedQuantity -= moved;
                        postedDetail = new BMWMS.Repository.Models.InboundOrderDetail
                        {
                            InboundOrderItemId = item.InboundOrderItemId,
                            InboundOrderId = order.InboundOrderId,
                            ProductId = item.ProductId,
                            StorageLocationId = destination.StorageLocationId,
                            ProductLotId = staging.ProductLotId,
                            ReceivedQuantity = moved,
                            ConditionStatus = "GOOD",
                            RecordedByUserId = currentUserId,
                            RecordedAt = DateTime.UtcNow,
                            Notes = "Putaway tách nhiều vị trí."
                        };
                        _context.InboundOrderDetails.Add(postedDetail);
                        item.InboundOrderDetails.Add(postedDetail);
                    }

                    var inventoryTransaction = new BMWMS.Repository.Models.InventoryTransaction
                    {
                        TransactionType = "INBOUND",
                        ProductId = item.ProductId,
                        StorageLocationId = destination.StorageLocationId,
                        ProductLotId = staging.ProductLotId,
                        OnHandDelta = moved,
                        ReservedDelta = 0,
                        InboundOrderDetail = postedDetail,
                        PerformedByUserId = currentUserId,
                        TransactionAt = DateTime.UtcNow,
                        Notes = $"Putaway từ {order.InboundOrderNumber} vào {destination.LocationCode}."
                    };
                    postedDetail.InventoryTransaction = inventoryTransaction;
                    _context.InventoryTransactions.Add(inventoryTransaction);
                    remaining -= moved;
                }
            }

            await _context.SaveChangesAsync();
            await dbTransaction.CommitAsync();
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            throw;
        }
    }

    private async Task EnsureAssignedUserAsync(long? assignedToUserId, long currentUserId, string action)
    {
        var user = await _context.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == currentUserId);
        if (user?.Status != "ACTIVE" || user.Role?.RoleCode != "WAREHOUSE_STAFF")
            throw new UnauthorizedAccessException($"Chỉ nhân viên kho đang hoạt động mới được {action}.");
        if (assignedToUserId != currentUserId)
            throw new UnauthorizedAccessException($"Bạn không được phân công để {action} cho phiếu này.");
    }

    private async Task EnsureWarehouseStaffAssigneeAsync(long? assignedToUserId, long? excludeInboundOrderId = null)
    {
        if (!assignedToUserId.HasValue) return;

        var isWarehouseStaff = await _context.Users.AnyAsync(u =>
            u.UserId == assignedToUserId.Value && u.Status == "ACTIVE" &&
            u.Role.RoleCode == "WAREHOUSE_STAFF" &&
            !u.InboundOrderAssignedToUsers.Any(o =>
                o.InboundOrderId != excludeInboundOrderId &&
                o.Status != "CANCELLED" && o.Status != "PUTAWAY_COMPLETED" &&
                (o.Status != "COMPLETED" ||
                 o.InboundOrderItems.SelectMany(i => i.InboundOrderDetails)
                     .Any(d => d.ConditionStatus == "GOOD" && d.InventoryTransaction == null))) &&
            !u.OutboundOrderAssignedToUsers.Any(o =>
                o.Status != "CANCELLED" && o.Status != "COMPLETED"));
        if (!isWarehouseStaff)
            throw new ArgumentException("Nhân viên không thuộc vai trò Nhân viên kho, đang bị khóa hoặc đang phụ trách một phiếu nhập/xuất khác.");
    }

    private async Task ValidateQuantitiesAsync(IEnumerable<(long ProductId, decimal Quantity, string FieldName)> values)
    {
        var lines = values.ToList();
        var productIds = lines.Select(x => x.ProductId).Distinct().ToList();
        var products = await _context.Products
            .Include(p => p.UnitOfMeasure)
            .Where(p => productIds.Contains(p.ProductId))
            .ToDictionaryAsync(p => p.ProductId);

        foreach (var line in lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product) || !IsActive(product.Status))
                throw new ArgumentException($"Sản phẩm ID {line.ProductId} không tồn tại hoặc đã ngừng hoạt động.");
            QuantityRules.EnsureValid(product, line.Quantity, line.FieldName);
        }
    }

    public async Task<List<PutawayLocationDto>> GetPutawayLocationsAsync(long warehouseId, long productId)
    {
        var locations = await _context.StorageLocations
            .Include(location => location.StorageRack)
                .ThenInclude(rack => rack!.WarehouseZone)
            .Include(location => location.Inventories)
            .Include(location => location.ProductFixedLocations)
            .Where(location => location.WarehouseId == warehouseId &&
                               location.LocationType == "BIN" && location.IsPutawayAllowed &&
                               (location.Status == "ACTIVE" || location.Status == "AVAILABLE" || location.Status == "OCCUPIED"))
            .ToListAsync();

        return locations
            .Select(location =>
            {
                var fixedLocation = location.ProductFixedLocations
                    .Where(fixedItem => fixedItem.ProductId == productId && fixedItem.IsActive)
                    .OrderByDescending(fixedItem => fixedItem.IsDefault)
                    .ThenBy(fixedItem => fixedItem.Priority)
                    .FirstOrDefault();
                return new PutawayLocationDto
                {
                    StorageLocationId = location.StorageLocationId,
                    LocationCode = location.LocationCode,
                    LocationName = location.LocationName ?? location.LocationCode,
                    ZoneId = location.StorageRack?.ZoneId,
                    ZoneCode = location.StorageRack?.WarehouseZone?.ZoneCode ?? string.Empty,
                    ZoneName = location.StorageRack?.WarehouseZone?.ZoneName ?? string.Empty,
                    RackId = location.RackId,
                    RackCode = location.StorageRack?.RackCode ?? string.Empty,
                    RackName = location.StorageRack?.RackName ?? string.Empty,
                    Status = location.Status,
                    CurrentOnHandQuantity = location.Inventories.Sum(inventory => inventory.OnHandQuantity),
                    StoredProductCount = location.Inventories
                        .Where(inventory => inventory.OnHandQuantity > 0)
                        .Select(inventory => inventory.ProductId)
                        .Distinct()
                        .Count(),
                    IsRecommended = fixedLocation != null,
                    Priority = fixedLocation?.Priority ?? int.MaxValue,
                    IsDefault = fixedLocation?.IsDefault ?? false
                };
            })
            .OrderByDescending(location => location.IsDefault)
            .ThenByDescending(location => location.IsRecommended)
            .ThenBy(location => location.Priority)
            .ThenBy(location => location.ZoneCode)
            .ThenBy(location => location.RackCode)
            .ThenBy(location => location.LocationCode)
            .ToList();
    }

    private static bool IsActive(string? status)
    {
        return status is "ACTIVE" or "AVAILABLE" or "OCCUPIED";
    }

    private static string GetInboundDisplayStatus(BMWMS.Repository.Models.InboundOrder order)
    {
        if (order.Status == "DRAFT") return "DRAFT";
        if (order.Status == "ASSIGNED") return "READY";
        if (order.Status == "IN_PROGRESS") return "RECEIVING";
        if (order.Status == "CANCELLED") return "CANCELLED";

        var goodDetails = order.InboundOrderItems.SelectMany(i => i.InboundOrderDetails)
            .Where(d => d.ConditionStatus == "GOOD").ToList();
        return (goodDetails.Count == 0 || goodDetails.All(d => d.InventoryTransaction?.TransactionType == "INBOUND"))
            ? "PUTAWAY_COMPLETED"
            : "RECEIVED";
    }

}
