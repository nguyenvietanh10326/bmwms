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

    public async Task<ProductStatisticsResponseDto> GetProductStatisticsAsync(ProductStatisticsFilterDto filter)
    {
        var (totalCount, items) = await _reportRepository.GetProductStatisticsAsync(
            filter.FromDate, filter.ToDate, filter.ProductSearch, filter.ProductGroupCode, filter.PageNumber, filter.PageSize);

        return new ProductStatisticsResponseDto
        {
            CutOffTime = DateTime.UtcNow,
            Data = new PagedResultDto<ProductStatisticsItemDto>
            {
                Items = items.Select(x => new ProductStatisticsItemDto
                {
                    ProductId = x.ProductId,
                    ProductCode = x.ProductCode,
                    ProductName = x.ProductName,
                    BaseUnitCode = x.BaseUnitCode,
                    CurrentStock = x.CurrentStock,
                    InboundQuantity = x.InboundQuantity,
                    OutboundQuantity = x.OutboundQuantity,
                    AdjustmentQuantity = x.AdjustmentQuantity,
                    MovementFrequency = x.MovementFrequency,
                    DaysSinceLastMovement = x.DaysSinceLastMovement
                }).ToList(),
                TotalCount = totalCount,
                PageIndex = filter.PageNumber,
                PageSize = filter.PageSize
            }
        };
    }

    public async Task<SupplierStatisticsResponseDto> GetSupplierStatisticsAsync(SupplierStatisticsFilterDto filter)
    {
        var (totalCount, items) = await _reportRepository.GetSupplierStatisticsAsync(
            filter.FromDate, filter.ToDate, filter.SupplierSearch, filter.PageNumber, filter.PageSize);

        return new SupplierStatisticsResponseDto
        {
            CutOffTime = DateTime.UtcNow,
            Data = new PagedResultDto<SupplierStatisticsItemDto>
            {
                Items = items.Select(x => new SupplierStatisticsItemDto
                {
                    SupplierId = x.SupplierId,
                    SupplierCode = x.SupplierCode,
                    SupplierName = x.SupplierName,
                    InboundOrderCount = x.InboundOrderCount,
                    ExpectedQuantity = x.ExpectedQuantity,
                    ReceivedQuantity = x.ReceivedQuantity,
                    DamagedQuantity = x.DamagedQuantity,
                    ShortageQuantity = x.ShortageQuantity
                }).ToList(),
                TotalCount = totalCount,
                PageIndex = filter.PageNumber,
                PageSize = filter.PageSize
            }
        };
    }

    public async Task<StocktakeStatisticsResponseDto> GetStocktakeStatisticsAsync(StocktakeStatisticsFilterDto filter)
    {
        var (totalCount, items) = await _reportRepository.GetStocktakeStatisticsAsync(
            filter.FromDate, filter.ToDate, filter.CountType, filter.StorageAreaCode, filter.ProductGroupCode, filter.SessionStatus, filter.PageNumber, filter.PageSize);

        return new StocktakeStatisticsResponseDto
        {
            CutOffTime = DateTime.UtcNow,
            Data = new PagedResultDto<StocktakeStatisticsItemDto>
            {
                Items = items.Select(x => new StocktakeStatisticsItemDto
                {
                    StocktakeSessionId = x.StocktakeSessionId,
                    StocktakeNumber = x.StocktakeNumber,
                    PlannedDate = x.PlannedDate,
                    Status = x.Status,
                    BinsCounted = x.BinsCounted,
                    MatchedItems = x.MatchedItems,
                    ShortageItems = x.ShortageItems,
                    ExcessItems = x.ExcessItems,
                    TotalItemsCounted = x.TotalItemsCounted,
                    TotalShortageQuantity = x.TotalShortageQuantity,
                    TotalExcessQuantity = x.TotalExcessQuantity,
                    TotalApprovedAdjustmentQuantity = x.TotalApprovedAdjustmentQuantity
                }).ToList(),
                TotalCount = totalCount,
                PageIndex = filter.PageNumber,
                PageSize = filter.PageSize
            }
        };
    }

    public async Task<LowStockAlertResponseDto> GetLowStockAlertsAsync(LowStockAlertFilterDto filter)
    {
        var result = await _reportRepository.GetLowStockAlertsAsync(
            filter.Keyword,
            filter.PageNumber,
            filter.PageSize);

        var items = result.Items.Select(x => new LowStockAlertItemDto
        {
            WarehouseId = x.WarehouseId,
            WarehouseCode = x.WarehouseCode,
            ProductId = x.ProductId,
            ProductCode = x.ProductCode,
            ProductName = x.ProductName,
            MinimumStockQuantity = x.MinimumStockQuantity,
            AvailableQuantity = x.AvailableQuantity,
            ShortageQuantity = x.ShortageQuantity
        }).ToList();

        var response = new LowStockAlertResponseDto
        {
            Data = new PagedResultDto<LowStockAlertItemDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                PageIndex = filter.PageNumber,
                PageSize = filter.PageSize
            },
            CutOffTime = DateTime.UtcNow
        };

        return response;
    }

    public async Task<ExpiringLotAlertResponseDto> GetExpiringLotAlertsAsync(ExpiringLotAlertFilterDto filter)
    {
        var result = await _reportRepository.GetExpiringLotAlertsAsync(
            filter.Keyword,
            filter.MaxDaysToExpiry,
            filter.PageNumber,
            filter.PageSize);

        var items = result.Items.Select(x => new ExpiringLotAlertItemDto
        {
            WarehouseId = x.WarehouseId,
            WarehouseCode = x.WarehouseCode,
            ProductId = x.ProductId,
            ProductCode = x.ProductCode,
            ProductName = x.ProductName,
            ProductLotId = x.ProductLotId,
            LotNumber = x.LotNumber,
            ExpiryDate = x.ExpiryDate,
            DaysToExpiry = x.DaysToExpiry,
            OnHandQuantity = x.OnHandQuantity,
            AvailableQuantity = x.AvailableQuantity,
            ExpiryWarningDays = x.ExpiryWarningDays
        }).ToList();

        var response = new ExpiringLotAlertResponseDto
        {
            Data = new PagedResultDto<ExpiringLotAlertItemDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                PageIndex = filter.PageNumber,
                PageSize = filter.PageSize
            },
            CutOffTime = DateTime.UtcNow
        };

        return response;
    }

    public async Task<OverdueOrderAlertResponseDto> GetOverdueOrderAlertsAsync(OverdueOrderAlertFilterDto filter)
    {
        var result = await _reportRepository.GetOverdueOrderAlertsAsync(
            filter.DocumentType,
            filter.Keyword,
            filter.PageNumber,
            filter.PageSize);

        var items = result.Items.Select(x => new OverdueOrderAlertItemDto
        {
            DocumentType = x.DocumentType,
            DocumentId = x.DocumentId,
            DocumentNumber = x.DocumentNumber,
            WarehouseId = x.WarehouseId,
            DueDate = x.DueDate,
            DaysOverdue = x.DaysOverdue,
            Status = x.Status
        }).ToList();

        var response = new OverdueOrderAlertResponseDto
        {
            Data = new PagedResultDto<OverdueOrderAlertItemDto>
            {
                Items = items,
                TotalCount = result.TotalCount,
                PageIndex = filter.PageNumber,
                PageSize = filter.PageSize
            },
            CutOffTime = DateTime.UtcNow
        };

        return response;
    }
}
