using BMWMS.Web.Models;

namespace BMWMS.Web.Services;

public interface IReportApiService
{
    Task<InventoryReportResponseModel?> GetInventoryReportAsync(InventoryReportFilterModel filter);
    Task<InboundReportResponseModel> GetInboundReportAsync(InboundReportFilterModel filter);
}
