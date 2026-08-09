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
}
