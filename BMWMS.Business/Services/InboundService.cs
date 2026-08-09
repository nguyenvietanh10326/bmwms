using System;
using System.Linq;
using System.Threading.Tasks;
using BMWMS.Business.DTOs.Inbound;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Interfaces;

namespace BMWMS.Business.Services;

public class InboundService : IInboundService
{
    private readonly IInboundRepository _inboundRepository;

    public InboundService(IInboundRepository inboundRepository)
    {
        _inboundRepository = inboundRepository;
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
}
