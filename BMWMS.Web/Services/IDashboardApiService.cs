using BMWMS.Web.Models; // Note: Need to duplicate DTOs in Web/Models or reference Business? Usually Web has its own Models.
// Let's create DashboardModels.cs

namespace BMWMS.Web.Services;

public interface IDashboardApiService
{
    Task<DashboardResponseModel?> GetDashboardDataAsync();
}
