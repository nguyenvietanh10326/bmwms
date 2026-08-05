using BMWMS.Business.DTOs.Report;

namespace BMWMS.Business.Interfaces;

public interface IReportService
{
    Task<InventoryReportResponseDto> GetInventoryReportAsync(InventoryReportFilterDto filter);
    Task<InboundReportResponseDto> GetInboundReportAsync(InboundReportFilterDto filter);
    Task<OutboundReportResponseDto> GetOutboundReportAsync(OutboundReportFilterDto filter);
    Task<InOutStockReportResponseDto> GetInOutStockReportAsync(InOutStockReportFilterDto filter);
    Task<ProductStatisticsResponseDto> GetProductStatisticsAsync(ProductStatisticsFilterDto filter);
    Task<SupplierStatisticsResponseDto> GetSupplierStatisticsAsync(SupplierStatisticsFilterDto filter);
    Task<StocktakeStatisticsResponseDto> GetStocktakeStatisticsAsync(StocktakeStatisticsFilterDto filter);
    Task<LowStockAlertResponseDto> GetLowStockAlertsAsync(LowStockAlertFilterDto filter);
    Task<ExpiringLotAlertResponseDto> GetExpiringLotAlertsAsync(ExpiringLotAlertFilterDto filter);
    Task<OverdueOrderAlertResponseDto> GetOverdueOrderAlertsAsync(OverdueOrderAlertFilterDto filter);
}
