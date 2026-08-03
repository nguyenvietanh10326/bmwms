using BMWMS.Business.DTOs.Dashboard;

namespace BMWMS.Business.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponseDto> GetDashboardDataAsync(long? warehouseId = null);
}
