using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Report;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Interfaces;

namespace BMWMS.Business.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;

    public ReportService(IReportRepository reportRepository)
    {
        _reportRepository = reportRepository;
    }

    public async Task<InboundReportResponseDto> GetInboundReportAsync(InboundReportFilterDto filter)
    {
        var (totalCount, totalExpected, totalReceived, totalDamaged, totalShortage, items) = await _reportRepository.GetInboundReportAsync(
            filter.FromDate,
            filter.ToDate,
            filter.ProductSearch,
            filter.Status,
            filter.PageNumber,
            filter.PageSize
        );

        return new InboundReportResponseDto
        {
            TotalExpected = totalExpected,
            TotalReceived = totalReceived,
            TotalDamaged = totalDamaged,
            TotalShortage = totalShortage,
            TotalCount = totalCount,
            Items = items.Select(x => new InboundReportItemDto
            {
                InboundOrderNumber = x.InboundOrderNumber,
                ProductCode = x.ProductCode,
                ProductName = x.ProductName,
                ExpectedReceiptDate = x.ExpectedReceiptDate,
                ExpectedQuantity = x.ExpectedQuantity,
                ReceivedQuantity = x.ReceivedQuantity,
                DamagedQuantity = x.DamagedQuantity,
                ShortageQuantity = x.ShortageQuantity,
                Status = x.Status
            }).ToList()
        };
    }

    public async Task<OutboundReportResponseDto> GetOutboundReportAsync(OutboundReportFilterDto filter)
    {
        var (totalCount, totalRequested, totalIssued, items) = await _reportRepository.GetOutboundReportAsync(
            filter.FromDate, filter.ToDate, filter.ProductSearch, filter.Status, filter.PageNumber, filter.PageSize);

        return new OutboundReportResponseDto
        {
            TotalRequested = totalRequested,
            TotalIssued = totalIssued,
            TotalCount = totalCount,
            Items = items.Select(x => new OutboundReportItemDto
            {
                OutboundOrderNumber = x.OutboundOrderNumber,
                ProductCode = x.ProductCode,
                ProductName = x.ProductName,
                SourceType = x.SourceType,
                ExpectedIssueDate = x.ExpectedIssueDate.ToDateTime(TimeOnly.MinValue),
                RequestedQuantity = x.RequestedQuantity,
                IssuedQuantity = x.IssuedQuantity,
                Status = x.Status
            }).ToList()
        };
    }

    public async Task<InOutStockReportResponseDto> GetInOutStockReportAsync(InOutStockReportFilterDto filter)
    {
        var (totalCount, items) = await _reportRepository.GetInOutStockReportAsync(
            filter.FromDate, filter.ToDate, filter.ProductSearch, filter.PageNumber, filter.PageSize);

        return new InOutStockReportResponseDto
        {
            TotalCount = totalCount,
            Items = items.Select(x => new InOutStockReportItemDto
            {
                ProductCode = x.ProductCode,
                ProductName = x.ProductName,
                OpeningBalance = x.OpeningBalance,
                InboundQuantity = x.InboundQuantity,
                OutboundQuantity = x.OutboundQuantity,
                AdjustmentQuantity = x.AdjustmentQuantity,
                ClosingBalance = x.ClosingBalance
            }).ToList()
        };
    }

    public async Task<InventoryReportResponseDto> GetInventoryReportAsync(InventoryReportFilterDto filter)
    {
        var (items, totalCount, totalOnHand, totalReserved, totalAvailable) = await _reportRepository.GetInventoryReportAsync(
            filter.ProductSearch,
            filter.LocationCode,
            filter.LotNumber,
            filter.PositiveStockOnly,
            filter.PageIndex,
            filter.PageSize);

        var dtos = items.Select(i => new InventoryReportItemDto
        {
            ProductCode = i.ProductCode,
            ProductName = i.ProductName,
            UnitCode = i.UnitCode,
            LotNumber = i.LotNumber,
            LocationCode = i.LocationCode,
            ExpiryDate = i.ExpiryDate,
            OnHandQuantity = i.OnHandQuantity,
            ReservedQuantity = i.ReservedQuantity,
            AvailableQuantity = i.AvailableQuantity ?? 0
        }).ToList();

        var pagedResult = new PagedResultDto<InventoryReportItemDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize
        };

        return new InventoryReportResponseDto
        {
            Data = pagedResult,
            TotalOnHand = totalOnHand,
            TotalReserved = totalReserved,
            TotalAvailable = totalAvailable,
            CutOffTime = DateTime.UtcNow
        };
    }
}
