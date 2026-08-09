using System;
using System.Linq;
using System.Threading.Tasks;
using BMWMS.Business.DTOs.Inbound;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Business.Services;

public class InboundService : IInboundService
{
    private readonly IInboundRepository _inboundRepository;
    private readonly BMWMS.Repository.Models.BmwmsContext _context;

    public InboundService(IInboundRepository inboundRepository, BMWMS.Repository.Models.BmwmsContext context)
    {
        _inboundRepository = inboundRepository;
        _context = context;
    }

    public async Task<InboundOrderPageDto> GetInboundOrdersPageAsync(InboundOrderFilterDto filter)
    {
        var result = await _inboundRepository.GetInboundOrdersPageAsync(
            filter.Keyword,
            filter.Status,
            filter.FromDate,
            filter.ToDate,
            filter.PageIndex,
            filter.PageSize);

        return new InboundOrderPageDto
        {
            TotalCount = result.TotalCount,
            Items = result.Items.Select(x => new InboundOrderListDto
            {
                InboundOrderId = x.InboundOrderId,
                InboundOrderNumber = x.InboundOrderNumber,
                SupplierName = x.PurchaseOrder?.Supplier?.SupplierName ?? "N/A",
                ExpectedReceiptDate = x.ExpectedReceiptDate,
                Status = x.Status,
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
            SupplierName = order.PurchaseOrder?.Supplier?.SupplierName ?? "",
            WarehouseName = order.Warehouse?.WarehouseName ?? "",
            ExpectedReceiptDate = order.ExpectedReceiptDate,
            Status = order.Status,
            AssignedToUserName = order.AssignedToUser?.FullName ?? "",
            CreatedByUserName = order.CreatedByUser?.FullName ?? "",
            ParentInboundOrderNumber = order.ParentInboundOrder?.InboundOrderNumber,
            Items = order.InboundOrderItems.Select(item => new InboundOrderItemDto
            {
                InboundOrderItemId = item.InboundOrderItemId,
                ProductCode = item.Product.ProductCode,
                ProductName = item.Product.ProductName,
                ExpectedQuantity = item.ExpectedQuantity,
                ReceivedQuantity = item.ReceivedQuantity,
                PutawayQuantity = item.InboundOrderDetails.Sum(d => d.ReceivedQuantity),
                // Lấy Lot đầu tiên nếu có (vì hiển thị chi tiết thì 1 dòng thường ứng với 1 lô, hoặc list các lô. Ở đây ta lấy lô mới nhất)
                LotNumber = item.InboundOrderDetails.FirstOrDefault()?.ProductLot?.LotNumber,
                ExpiryDate = item.InboundOrderDetails.FirstOrDefault()?.ProductLot?.ExpiryDate
            }).ToList()
        };

        // Tạo Timeline giả lập từ các mốc thời gian của InboundOrder
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
        if (dto.Items == null || !dto.Items.Any())
            throw new ArgumentException("Lệnh nhập kho phải có ít nhất 1 dòng hàng.");

        if (dto.Items.Any(i => i.ExpectedQuantity <= 0))
            throw new ArgumentException("Số lượng dự kiến phải lớn hơn 0.");

        if (dto.SourceType == "PURCHASE_ORDER" && dto.PurchaseOrderId.HasValue)
        {
            var po = await GetPurchaseOrderForInboundAsync(dto.PurchaseOrderId.Value);
            foreach (var item in dto.Items)
            {
                var poItem = po?.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (poItem == null || item.ExpectedQuantity > poItem.RemainingQuantity)
                    throw new ArgumentException($"Số lượng dự kiến vượt quá số lượng còn lại.");
            }
        }
        else if (dto.SourceType == "SUPPLEMENT" && dto.ParentInboundOrderId.HasValue)
        {
            var parent = await GetInboundOrderForSupplementAsync(dto.ParentInboundOrderId.Value);
            foreach (var item in dto.Items)
            {
                var parentItem = parent?.Items.FirstOrDefault(i => i.ProductId == item.ProductId);
                if (parentItem == null || item.ExpectedQuantity > parentItem.RemainingQuantity)
                    throw new ArgumentException($"Số lượng dự kiến vượt quá số lượng thiếu.");
            }
        }

        var inboundOrder = new BMWMS.Repository.Models.InboundOrder
        {
            InboundOrderNumber = "IN-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"),
            SourceType = dto.SourceType,
            PurchaseOrderId = dto.PurchaseOrderId,
            SalesOrderId = dto.SalesOrderId,
            WarehouseId = dto.WarehouseId,
            ExpectedReceiptDate = dto.ExpectedReceiptDate,
            Notes = dto.Notes,
            Status = "DRAFT", // Trạng thái mặc định
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = currentUserId,
            AssignedToUserId = dto.AssignedToUserId,
            ParentInboundOrderId = dto.ParentInboundOrderId
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
                ProductName = detail.Product.ProductName,
                UnitName = detail.Product.UnitOfMeasure?.UnitName ?? string.Empty,
                OrderedQuantity = detail.OrderedQuantity,
                InboundQuantity = totalInbound,
                RemainingQuantity = remaining > 0 ? remaining : 0
            });
        }

        return dto;
    }

    public async Task<List<ShortageInboundOrderDto>> GetShortageInboundOrdersAsync()
    {
        var shortageOrders = await _context.InboundOrders
            .Include(o => o.PurchaseOrder)
                .ThenInclude(po => po.Supplier)
            .Include(o => o.InboundOrderItems)
            .Where(o => o.Status == "COMPLETED" 
                     && o.InboundOrderItems.Any(i => i.ReceivedQuantity < i.ExpectedQuantity))
            .ToListAsync();

        return shortageOrders.Select(o => new ShortageInboundOrderDto
        {
            InboundOrderId = o.InboundOrderId,
            InboundOrderNumber = o.InboundOrderNumber,
            PurchaseOrderNumber = o.PurchaseOrder?.PurchaseOrderNumber ?? string.Empty,
            SupplierName = o.PurchaseOrder?.Supplier?.SupplierName ?? string.Empty,
            OriginalExpectedDate = o.ExpectedReceiptDate
        }).ToList();
    }

    public async Task<PurchaseOrderForInboundDto?> GetInboundOrderForSupplementAsync(long parentId)
    {
        var parentOrder = await _context.InboundOrders
            .Include(o => o.PurchaseOrder)
                .ThenInclude(po => po.Supplier)
            .Include(o => o.InboundOrderItems)
                .ThenInclude(i => i.Product)
                    .ThenInclude(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(o => o.InboundOrderId == parentId);

        if (parentOrder == null) return null;

        var dto = new PurchaseOrderForInboundDto
        {
            PurchaseOrderId = parentOrder.PurchaseOrderId ?? 0,
            PurchaseOrderNumber = parentOrder.InboundOrderNumber, // Use IO number for reference
            SupplierName = parentOrder.PurchaseOrder?.Supplier?.SupplierName ?? string.Empty
        };

        foreach (var item in parentOrder.InboundOrderItems)
        {
            var shortage = item.ExpectedQuantity - item.ReceivedQuantity;
            if (shortage > 0)
            {
                // Find existing supplements for this parent
                var existingSupplements = await _context.InboundOrderItems
                    .Include(i => i.InboundOrder)
                    .Where(i => i.InboundOrder.ParentInboundOrderId == parentId 
                             && i.InboundOrder.Status != "CANCELLED"
                             && i.ProductId == item.ProductId)
                    .SumAsync(i => i.ExpectedQuantity);

                var remaining = shortage - existingSupplements;

                dto.Items.Add(new PurchaseOrderItemForInboundDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.ProductName,
                    UnitName = item.Product.UnitOfMeasure?.UnitName ?? string.Empty,
                    OrderedQuantity = shortage, // Treat the initial shortage as ordered
                    InboundQuantity = existingSupplements,
                    RemainingQuantity = remaining > 0 ? remaining : 0
                });
            }
        }

        return dto;
    }

    public async Task<List<SourceOrderDropdownDto>> GetPendingPurchaseOrdersAsync()
    {
        return await _context.PurchaseOrders
            .Include(po => po.Supplier)
            .Where(po => po.Status == "CONFIRMED" || po.Status == "PARTIALLY_RECEIVED")
            .Select(po => new SourceOrderDropdownDto
            {
                Id = po.PurchaseOrderId,
                Name = $"{po.PurchaseOrderNumber} — {po.Supplier.SupplierName}"
            })
            .ToListAsync();
    }

    public async Task<List<SourceOrderDropdownDto>> GetReturnableSalesOrdersAsync()
    {
        return await _context.SalesOrders
            .Include(so => so.Customer)
            .Where(so => so.Status == "FULFILLED" || so.Status == "PARTIALLY_FULFILLED")
            .Select(so => new SourceOrderDropdownDto
            {
                Id = so.SalesOrderId,
                Name = $"{so.SalesOrderNumber} — {so.Customer.CustomerName}"
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
                    ProductName = d.Product.ProductName,
                    UnitName = d.Product.UnitOfMeasure?.UnitName ?? "",
                    OrderedQuantity = d.FulfilledQuantity, // Gán tạm OrderedQuantity = FulfilledQuantity để frontend hiển thị đúng
                    RemainingQuantity = remaining > 0 ? remaining : 0
                };
            }).Where(i => i.RemainingQuantity > 0).ToList() // Chỉ lấy món còn hàng để nhập kho
        };

        return dto;
    }
}
