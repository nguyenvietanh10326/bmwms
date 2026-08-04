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
        var (items, totalExpected, totalReceived, totalDamaged, totalShortage) = await _reportRepository.GetInboundReportAsync(
            filter.FromDate,
            filter.ToDate,
            filter.ProductSearch,
            filter.Status
        );

        var dtos = items.Select(i => new InboundReportItemDto
        {
            InboundOrderNumber = i.InboundOrderNumber,
            ProductCode = i.ProductCode,
            ProductName = i.ProductName,
            SourceType = i.SourceType,
            ExpectedReceiptDate = i.ExpectedReceiptDate,
            Status = i.Status,
            ExpectedQuantity = i.ExpectedQuantity,
            ReceivedQuantity = i.ReceivedQuantity,
            DamagedQuantity = i.DamagedQuantity,
            ShortageQuantity = i.ShortageQuantity
        }).ToList();

        return new InboundReportResponseDto
        {
            TotalExpected = totalExpected,
            TotalReceived = totalReceived,
            TotalDamaged = totalDamaged,
            TotalShortage = totalShortage,
            Items = dtos
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
