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
            SalesOrderNumber = order.SalesOrder?.SalesOrderNumber,
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
            TransferOrderId = order.TransferOrderId,
            TransferOrderNumber = order.TransferOrder?.TransferOrderNumber,
            ParentInboundOrderNumber = order.ParentInboundOrder?.InboundOrderNumber,
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
                LotNumber = i.InboundOrderDetails.FirstOrDefault()?.ProductLot?.LotNumber,
                ExpiryDate = i.InboundOrderDetails.FirstOrDefault()?.ProductLot?.ExpiryDate,
                Receipts = i.InboundOrderDetails.Select(d => new BMWMS.Business.DTOs.Inbound.InboundReceiptDto
                {
                    InboundOrderDetailId = d.InboundOrderDetailId,
                    ProductLotId = d.ProductLotId,
                    LotNumber = d.ProductLot?.LotNumber ?? string.Empty,
                    ExpiryDate = d.ProductLot?.ExpiryDate,
                    ReceivedQuantity = d.ReceivedQuantity,
                    PutawayQuantity = d.InventoryTransaction?.TransactionType == "INBOUND" ? d.ReceivedQuantity : 0,
                    ConditionStatus = d.ConditionStatus,
                    LocationCode = d.StorageLocation?.LocationCode ?? string.Empty
                }).ToList()
            }).ToList()
        };

        var timeline = new List<InboundOrderTimelineDto>();
        
        timeline.Add(new InboundOrderTimelineDto
        {
            EventTime = order.CreatedAt,
            EventName = "Tạo lệnh nhập",
            StatusBadge = "READY",
            StatusBadgeColor = "gy",
            PerformedBy = order.CreatedByUser?.FullName ?? "Hệ thống"
        });

        if (order.ConfirmedAt.HasValue)
        {
            timeline.Add(new InboundOrderTimelineDto
            {
                EventTime = order.ConfirmedAt.Value,
                EventName = "Xác nhận lệnh nhập",
                StatusBadge = "ASSIGNED",
                StatusBadgeColor = "b",
                PerformedBy = order.ConfirmedByUser?.FullName ?? "Quản lý"
            });
        }
        
        if (order.Status == "RECEIVED" || order.Status == "PUTAWAY_COMPLETED")
        {
            timeline.Add(new InboundOrderTimelineDto
            {
                EventTime = order.CreatedAt.AddHours(2), // Giả lập thời gian
                EventName = "Nhận hàng hoàn tất",
                StatusBadge = "RECEIVED",
                StatusBadgeColor = "g",
                PerformedBy = order.AssignedToUser?.FullName ?? ""
            });
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

        await EnsureWarehouseStaffAssigneeAsync(dto.AssignedToUserId);
        await ValidateQuantitiesAsync(dto.Items.Select(i => (i.ProductId, i.ExpectedQuantity, "Số lượng dự kiến")));

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
                if (poItem == null || item.ExpectedQuantity > poItem.RemainingQuantity)
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
            Status = dto.AssignedToUserId.HasValue ? "ASSIGNED" : "DRAFT",
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = currentUserId,
            AssignedToUserId = dto.AssignedToUserId,
            ParentInboundOrderId = null
        };

        var workflowTransfer = new BMWMS.Repository.Models.TransferOrder
        {
            TransferOrderNumber = $"TR-WF-IN-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            TransferType = "INTERNAL_LOCATION",
            SourceWarehouseId = dto.WarehouseId,
            DestinationWarehouseId = dto.WarehouseId,
            RequestedDate = dto.ExpectedReceiptDate,
            DueDate = dto.ExpectedReceiptDate,
            Status = dto.AssignedToUserId.HasValue ? "ASSIGNED" : "DRAFT",
            Notes = $"[AUTO_INBOUND] Phiếu tác nghiệp vị trí cho {inboundOrder.InboundOrderNumber}",
            CreatedByUserId = currentUserId,
            AssignedToUserId = dto.AssignedToUserId,
            CreatedAt = DateTime.UtcNow
        };
        inboundOrder.TransferOrder = workflowTransfer;

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
            workflowTransfer.TransferOrderDetails.Add(new BMWMS.Repository.Models.TransferOrderDetail
            {
                ProductId = itemDto.ProductId,
                RequestedQuantity = itemDto.ExpectedQuantity,
                MovedQuantity = 0,
                StaffNote = "Chờ nhận hàng và xác định lô/vị trí đích."
            });
        }

        await _inboundRepository.AddAsync(inboundOrder);
        await _inboundRepository.SaveChangesAsync();
        await assignmentTransaction.CommitAsync();

        if (inboundOrder.AssignedToUserId.HasValue)
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
                             !o.InboundOrderItems.SelectMany(i => i.InboundOrderDetails)
                                 .Any(d => d.ConditionStatus == "GOOD") ||
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
            .FirstOrDefaultAsync(o => o.InboundOrderId == id);

        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho");
        if (order.Status != "DRAFT") throw new Exception("Chỉ có thể sửa lệnh nhập kho ở trạng thái Chờ xử lý");

        if (dto.Items == null || !dto.Items.Any())
            throw new Exception("Lệnh nhập kho phải có ít nhất 1 dòng hàng.");

        if (dto.Items.Any(i => i.ExpectedQuantity <= 0))
            throw new Exception("Số lượng dự kiến phải lớn hơn 0.");

        await EnsureWarehouseStaffAssigneeAsync(dto.AssignedToUserId);
        await ValidateQuantitiesAsync(dto.Items.Select(i => (i.ProductId, i.ExpectedQuantity, "Số lượng dự kiến")));

        if (order.SourceType == "PURCHASE_ORDER" && order.PurchaseOrderId.HasValue)
        {
            var po = await GetPurchaseOrderForInboundAsync(order.PurchaseOrderId.Value);
            foreach (var item in dto.Items)
            {
                var poItem = po?.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (poItem == null || item.ExpectedQuantity > poItem.RemainingQuantity)
                    throw new Exception($"Số lượng dự kiến của sản phẩm {poItem?.ProductName ?? item.ProductId.ToString()} vượt quá số lượng còn lại ({poItem?.RemainingQuantity ?? 0}).");
            }
        }

        order.ExpectedReceiptDate = dto.ExpectedReceiptDate;
        order.Notes = dto.Notes;
        
        bool newlyAssigned = false;
        if (order.Status == "DRAFT" && dto.AssignedToUserId.HasValue)
        {
            order.Status = "ASSIGNED";
            newlyAssigned = true;
        }
        
        order.AssignedToUserId = dto.AssignedToUserId;

        foreach (var itemDto in dto.Items)
        {
            var existingItem = order.InboundOrderItems.FirstOrDefault(i => i.ProductId == itemDto.ProductId);
            if (existingItem != null)
            {
                existingItem.ExpectedQuantity = itemDto.ExpectedQuantity;
                existingItem.Notes = itemDto.Notes;
            }
        }

        await _context.SaveChangesAsync();
        await assignmentTransaction.CommitAsync();

        if (newlyAssigned && order.AssignedToUserId.HasValue)
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
        var order = await _inboundRepository.GetByIdAsync(id);
        if (order == null) throw new Exception("Không tìm thấy lệnh nhập kho");
        if (order.Status != "DRAFT") throw new Exception("Chỉ có thể hủy lệnh nhập kho ở trạng thái Chờ xử lý");

        order.Status = "CANCELLED";
        order.CancellationReason = dto.CancellationReason;
        order.CancelledAt = DateTime.UtcNow;
        order.CancelledByUserId = currentUserId;
        if (order.TransferOrder != null)
        {
            order.TransferOrder.Status = "CANCELLED";
            order.TransferOrder.ConfirmedAt = DateTime.UtcNow;
            order.TransferOrder.ConfirmedByUserId = currentUserId;
        }

        await _inboundRepository.UpdateAsync(order);
        await _inboundRepository.SaveChangesAsync();
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
        if (order.TransferOrder != null)
        {
            order.TransferOrder.Status = "ASSIGNED";
            order.TransferOrder.AssignedToUserId = order.AssignedToUserId;
            order.TransferOrder.ConfirmedAt = DateTime.UtcNow;
            order.TransferOrder.ConfirmedByUserId = currentUserId;
        }

        await _inboundRepository.UpdateAsync(order);
        await _inboundRepository.SaveChangesAsync();
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
        var lot = await _context.ProductLots
            .FirstOrDefaultAsync(l => l.ProductId == item.ProductId && l.LotNumber == dto.LotNumber);

        if (lot == null)
        {
            lot = new BMWMS.Repository.Models.ProductLot
            {
                ProductId = item.ProductId,
                LotNumber = dto.LotNumber,
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

            var lot = await _context.ProductLots
                .FirstOrDefaultAsync(l => l.ProductId == item.ProductId && l.LotNumber == reqItem.LotNumber);

            if (lot == null)
            {
                lot = new BMWMS.Repository.Models.ProductLot
                {
                    ProductId = item.ProductId,
                    LotNumber = reqItem.LotNumber,
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
            .FirstOrDefaultAsync(po => po.PurchaseOrderId == purchaseOrderId.Value);

        if (purchaseOrder == null || NormalizePurchaseOrderStatus(purchaseOrder.Status) == "CANCELLED")
            return;

        var activeInboundItems = purchaseOrder.InboundOrders
            .Where(io => io.Status != "CANCELLED")
            .SelectMany(io => io.InboundOrderItems)
            .ToList();

        var hasReceivedQuantity = activeInboundItems.Any(item => item.ReceivedQuantity - item.DamagedQuantity > 0);
        var isFullyReceived = purchaseOrder.PurchaseOrderDetails.Count > 0
            && purchaseOrder.PurchaseOrderDetails.All(detail =>
                activeInboundItems
                    .Where(item => item.ProductId == detail.ProductId)
                    .Sum(item => item.ReceivedQuantity - item.DamagedQuantity) >= detail.OrderedQuantity);

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

    private async Task<List<long>> ReceiveBatchCoreAsync(
        long inboundOrderId,
        IEnumerable<ReceiveInboundItemDto> requests,
        long currentUserId)
    {
        var requestItems = requests.Where(x => x.DeliveredQuantity > 0).ToList();
        if (requestItems.Count == 0)
            throw new ArgumentException("Phải có ít nhất một dòng hàng có số lượng thực giao lớn hơn 0.");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.InboundOrders
                .Include(o => o.InboundOrderItems).ThenInclude(i => i.Product).ThenInclude(p => p.UnitOfMeasure)
                .Include(o => o.TransferOrder).ThenInclude(t => t!.TransferOrderDetails)
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

            var hasDamaged = requestItems.Any(x => x.DamagedQuantity > 0);
            var quarantine = hasDamaged
                ? await _context.StorageLocations.FirstOrDefaultAsync(l =>
                    l.WarehouseId == order.WarehouseId && l.LocationType == "QUARANTINE" &&
                    l.Status != "BLOCKED" && l.Status != "INACTIVE")
                : null;
            if (hasDamaged && quarantine == null)
                throw new InvalidOperationException("Kho chưa cấu hình vị trí QUARANTINE để cách ly hàng hỏng.");

            var lotIds = new List<long>();
            foreach (var request in requestItems)
            {
                if (request.DamagedQuantity < 0 || request.DamagedQuantity > request.DeliveredQuantity)
                    throw new ArgumentException("Số lượng hỏng phải từ 0 đến số lượng thực giao.");

                var item = order.InboundOrderItems.SingleOrDefault(i => i.InboundOrderItemId == request.InboundOrderItemId)
                    ?? throw new ArgumentException("Dòng hàng không thuộc phiếu nhập kho.");
                if (!IsActive(item.Product.Status))
                    throw new InvalidOperationException($"Sản phẩm {item.Product.ProductCode} đang ngừng hoạt động.");
                QuantityRules.EnsureValid(item.Product, request.DeliveredQuantity, "Số lượng thực giao");
                QuantityRules.EnsureValid(item.Product, request.DamagedQuantity, "Số lượng hỏng");
                if (item.ReceivedQuantity + request.DeliveredQuantity > item.ExpectedQuantity)
                    throw new ArgumentException($"Số lượng nhận của {item.Product.ProductCode} vượt số lượng dự kiến còn lại.");
                if (item.Product.TrackLot && string.IsNullOrWhiteSpace(request.LotNumber))
                    throw new ArgumentException($"Phải nhập số lô cho {item.Product.ProductCode}.");
                if (item.Product.TrackExpiry &&
                    (!request.ExpiryDate.HasValue || request.ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow)))
                    throw new ArgumentException($"Sản phẩm {item.Product.ProductCode} phải có hạn dùng trong tương lai.");

                var lotNumber = item.Product.TrackLot ? request.LotNumber.Trim() : $"NO-LOT-{item.ProductId}";
                var expiryDate = item.Product.TrackExpiry ? request.ExpiryDate : null;
                var lot = await _context.ProductLots.FirstOrDefaultAsync(l =>
                    l.ProductId == item.ProductId && l.LotNumber == lotNumber);
                if (lot == null)
                {
                    lot = new BMWMS.Repository.Models.ProductLot
                    {
                        ProductId = item.ProductId,
                        LotNumber = lotNumber,
                        ExpiryDate = expiryDate,
                        FirstReceivedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        Status = "AVAILABLE",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ProductLots.Add(lot);
                    await _context.SaveChangesAsync();
                }
                else if (lot.ExpiryDate != expiryDate)
                {
                    throw new ArgumentException($"Hạn dùng của lô {lotNumber} không khớp dữ liệu đã có.");
                }

                var accepted = request.DeliveredQuantity - request.DamagedQuantity;
                item.ReceivedQuantity += request.DeliveredQuantity;
                item.DamagedQuantity += request.DamagedQuantity;
                item.ShortageQuantity = Math.Max(0, item.ExpectedQuantity - item.ReceivedQuantity);

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
                        Notes = "Đã kiểm đếm; chờ xếp vào vị trí cố định."
                    });
                    UpsertInboundWorkflowDetail(order.TransferOrder, item.ProductId, lot.ProductLotId,
                        receiving.StorageLocationId, null, accepted, "Hàng đạt; chờ putaway.");
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
                        Notes = "Hàng hỏng được cách ly; chưa cộng vào tồn khả dụng."
                    });
                    UpsertInboundWorkflowDetail(order.TransferOrder, item.ProductId, lot.ProductLotId,
                        receiving.StorageLocationId, quarantine.StorageLocationId, request.DamagedQuantity,
                        "Hàng hỏng chuyển cách ly.", request.DamagedQuantity);
                }

                lotIds.Add(lot.ProductLotId);
            }

            order.Status = order.InboundOrderItems.All(i => i.ReceivedQuantity >= i.ExpectedQuantity)
                ? "COMPLETED"
                : "IN_PROGRESS";
            if (order.TransferOrder != null)
                order.TransferOrder.Status = "IN_PROGRESS";

            await UpdatePurchaseOrderReceiptStatusAsync(order.PurchaseOrderId);
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
            throw new ArgumentException("Danh sách putaway không hợp lệ.");

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.InboundOrders
                .Include(o => o.InboundOrderItems)
                    .ThenInclude(i => i.InboundOrderDetails)
                        .ThenInclude(d => d.InventoryTransaction)
                .Include(o => o.InboundOrderItems)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.UnitOfMeasure)
                .Include(o => o.TransferOrder).ThenInclude(t => t!.TransferOrderDetails)
                .FirstOrDefaultAsync(o => o.InboundOrderId == inboundOrderId)
                ?? throw new InvalidOperationException("Không tìm thấy phiếu nhập kho.");

            await EnsureAssignedUserAsync(order.AssignedToUserId, currentUserId, "xếp hàng vào vị trí");
            if (order.Status != "COMPLETED")
                throw new InvalidOperationException("Chỉ được putaway sau khi đã xác nhận nhận đủ hàng của phiếu.");

            foreach (var allocation in dtos)
            {
                var item = order.InboundOrderItems.SingleOrDefault(i => i.InboundOrderItemId == allocation.InboundOrderItemId)
                    ?? throw new ArgumentException("Dòng hàng không thuộc phiếu nhập kho.");
                QuantityRules.EnsureValid(item.Product, allocation.PutawayQuantity, "Số lượng xếp vị trí");
                var destination = await _context.StorageLocations.FirstOrDefaultAsync(l =>
                    l.StorageLocationId == allocation.StorageLocationId)
                    ?? throw new ArgumentException("Không tìm thấy vị trí đích.");

                if (destination.WarehouseId != order.WarehouseId || destination.LocationType != "BIN" ||
                    !destination.IsPutawayAllowed || !IsActive(destination.Status))
                    throw new InvalidOperationException($"Vị trí {destination.LocationCode} không hợp lệ để putaway.");

                var isFixed = await _context.ProductFixedLocations.AnyAsync(f =>
                    f.ProductId == item.ProductId && f.StorageLocationId == destination.StorageLocationId && f.IsActive);
                if (!isFixed)
                    throw new InvalidOperationException($"Vị trí {destination.LocationCode} chưa được cấu hình cố định cho sản phẩm.");

                var remaining = allocation.PutawayQuantity;
                var stagingDetails = item.InboundOrderDetails
                    .Where(d => d.ProductLotId == allocation.ProductLotId && d.ConditionStatus == "GOOD" &&
                                d.InventoryTransaction == null)
                    .OrderBy(d => d.RecordedAt)
                    .ToList();
                if (stagingDetails.Sum(d => d.ReceivedQuantity) < remaining)
                    throw new InvalidOperationException("Số lượng putaway vượt số hàng đạt đang chờ xếp vị trí.");

                foreach (var staging in stagingDetails.Where(_ => remaining > 0))
                {
                    var moved = Math.Min(staging.ReceivedQuantity, remaining);
                    var sourceLocationId = staging.StorageLocationId;
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
                    RecordInboundWorkflowMove(order.TransferOrder, item.ProductId, staging.ProductLotId,
                        sourceLocationId, destination.StorageLocationId, moved, currentUserId);
                    remaining -= moved;
                }
            }

            if (order.TransferOrder != null)
            {
                var allPutaway = order.InboundOrderItems.SelectMany(i => i.InboundOrderDetails)
                    .Where(d => d.ConditionStatus == "GOOD")
                    .All(d => d.InventoryTransaction != null);
                order.TransferOrder.Status = allPutaway ? "COMPLETED" : "IN_PROGRESS";
                if (allPutaway)
                {
                    order.TransferOrder.ConfirmedByUserId = currentUserId;
                    order.TransferOrder.ConfirmedAt = DateTime.UtcNow;
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
        if (user?.Role?.RoleCode == "WAREHOUSE_STAFF" && assignedToUserId != currentUserId)
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
                 !o.InboundOrderItems.SelectMany(i => i.InboundOrderDetails)
                     .Any(d => d.ConditionStatus == "GOOD") ||
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
        return await _context.ProductFixedLocations
            .Where(f => f.ProductId == productId && f.IsActive &&
                        f.StorageLocation.WarehouseId == warehouseId &&
                        f.StorageLocation.LocationType == "BIN" && f.StorageLocation.IsPutawayAllowed &&
                        (f.StorageLocation.Status == "ACTIVE" || f.StorageLocation.Status == "AVAILABLE" || f.StorageLocation.Status == "OCCUPIED"))
            .OrderByDescending(f => f.IsDefault).ThenBy(f => f.Priority).ThenBy(f => f.StorageLocation.LocationCode)
            .Select(f => new PutawayLocationDto
            {
                StorageLocationId = f.StorageLocationId,
                LocationCode = f.StorageLocation.LocationCode,
                LocationName = f.StorageLocation.LocationName ?? f.StorageLocation.LocationCode,
                Priority = f.Priority,
                IsDefault = f.IsDefault
            }).ToListAsync();
    }

    private static bool IsActive(string? status)
    {
        return status is "ACTIVE" or "AVAILABLE" or "OCCUPIED";
    }

    private static string GetInboundDisplayStatus(BMWMS.Repository.Models.InboundOrder order)
    {
        if (order.Status == "DRAFT") return "DRAFT";
        if (order.Status == "ASSIGNED") return "READY";
        if (order.Status == "IN_PROGRESS" && order.InboundOrderItems.All(i => i.ReceivedQuantity >= i.ExpectedQuantity))
            return "RECEIVED";
        if (order.Status == "IN_PROGRESS") return "RECEIVING";
        if (order.Status == "CANCELLED") return "CANCELLED";

        var goodDetails = order.InboundOrderItems.SelectMany(i => i.InboundOrderDetails)
            .Where(d => d.ConditionStatus == "GOOD").ToList();
        return goodDetails.Count > 0 && goodDetails.All(d => d.InventoryTransaction?.TransactionType == "INBOUND")
            ? "PUTAWAY_COMPLETED"
            : "RECEIVED";
    }

    private static void UpsertInboundWorkflowDetail(
        BMWMS.Repository.Models.TransferOrder? transfer,
        long productId,
        long productLotId,
        long sourceLocationId,
        long? destinationLocationId,
        decimal quantity,
        string note,
        decimal movedQuantity = 0)
    {
        if (transfer == null) return;
        var detail = transfer.TransferOrderDetails.FirstOrDefault(d =>
            d.ProductId == productId && !d.ProductLotId.HasValue && !d.SourceLocationId.HasValue);
        if (detail == null)
        {
            detail = new BMWMS.Repository.Models.TransferOrderDetail { ProductId = productId };
            transfer.TransferOrderDetails.Add(detail);
        }
        detail.ProductLotId = productLotId;
        detail.SourceLocationId = sourceLocationId;
        detail.DestinationLocationId = destinationLocationId;
        detail.RequestedQuantity = quantity;
        detail.MovedQuantity = movedQuantity;
        detail.StaffNote = note;
    }

    private static void RecordInboundWorkflowMove(
        BMWMS.Repository.Models.TransferOrder? transfer,
        long productId,
        long productLotId,
        long sourceLocationId,
        long destinationLocationId,
        decimal quantity,
        long userId)
    {
        if (transfer == null) return;
        var detail = transfer.TransferOrderDetails.FirstOrDefault(d =>
            d.ProductId == productId && d.ProductLotId == productLotId &&
            d.SourceLocationId == sourceLocationId && !d.DestinationLocationId.HasValue &&
            d.RequestedQuantity - d.MovedQuantity >= quantity);

        if (detail == null)
        {
            detail = new BMWMS.Repository.Models.TransferOrderDetail
            {
                ProductId = productId,
                ProductLotId = productLotId,
                SourceLocationId = sourceLocationId,
                RequestedQuantity = quantity
            };
            transfer.TransferOrderDetails.Add(detail);
        }
        else if (detail.RequestedQuantity > quantity && detail.MovedQuantity == 0)
        {
            detail.RequestedQuantity -= quantity;
            detail = new BMWMS.Repository.Models.TransferOrderDetail
            {
                ProductId = productId,
                ProductLotId = productLotId,
                SourceLocationId = sourceLocationId,
                RequestedQuantity = quantity
            };
            transfer.TransferOrderDetails.Add(detail);
        }

        detail.DestinationLocationId = destinationLocationId;
        detail.MovedQuantity = detail.RequestedQuantity;
        detail.ConfirmedByUserId = userId;
        detail.ConfirmedAt = DateTime.UtcNow;
        detail.StaffNote = "Đã hoàn tất putaway; tồn kho do bút toán INBOUND ghi nhận.";
    }
}
