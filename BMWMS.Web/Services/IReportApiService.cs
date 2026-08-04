using BMWMS.Web.Models;

namespace BMWMS.Web.Services;

public interface IReportApiService
{
    Task<InventoryReportResponseModel?> GetInventoryReportAsync(InventoryReportFilterModel filter);
    Task<InboundReportResponseModel> GetInboundReportAsync(InboundReportFilterModel filter);
    Task<OutboundReportResponseModel> GetOutboundReportAsync(OutboundReportFilterModel filter);
    Task<InOutStockReportResponseModel> GetInOutStockReportAsync(InOutStockReportFilterModel filter);
    Task<ProductStatisticsResponseModel> GetProductStatisticsAsync(ProductStatisticsFilterModel filter);
    Task<SupplierStatisticsResponseModel> GetSupplierStatisticsAsync(SupplierStatisticsFilterModel filter);
}
