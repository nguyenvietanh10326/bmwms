using BMWMS.Web.Models;

namespace BMWMS.Web.Services;

public interface IReportApiService
{
    Task<InventoryReportResponseModel?> GetInventoryReportAsync(InventoryReportFilterModel filter);
    Task<InboundReportResponseModel> GetInboundReportAsync(InboundReportFilterModel filter);
    Task<OutboundReportResponseModel> GetOutboundReportAsync(OutboundReportFilterModel filter);
    Task<InOutStockReportResponseModel> GetInOutStockReportAsync(InOutStockReportFilterModel filter);
    Task<SupplierStatisticsResponseModel> GetSupplierStatisticsAsync(SupplierStatisticsFilterModel filter);

    Task<byte[]> ExportInventoryAsync(InventoryReportFilterModel filter);
    Task<byte[]> ExportInboundAsync(InboundReportFilterModel filter);
    Task<byte[]> ExportOutboundAsync(OutboundReportFilterModel filter);
    Task<byte[]> ExportInOutStockAsync(InOutStockReportFilterModel filter);
    Task<byte[]> ExportSupplierStatisticsAsync(SupplierStatisticsFilterModel filter);

    Task<LowStockAlertResponseModel> GetLowStockAlertsAsync(LowStockAlertFilterModel filter);
    Task<byte[]> ExportLowStockAlertsAsync(LowStockAlertFilterModel filter);

    Task<ExpiringLotAlertResponseModel> GetExpiringLotAlertsAsync(ExpiringLotAlertFilterModel filter);
    Task<byte[]> ExportExpiringLotAlertsAsync(ExpiringLotAlertFilterModel filter);

}
