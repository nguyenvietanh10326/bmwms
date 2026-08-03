using BMWMS.Business.DTOs.Dashboard;
using BMWMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
// [Authorize] // Comment tạm thời nếu chưa test auth
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardResponseDto>> GetDashboardData([FromQuery] long? warehouseId = null)
    {
        try
        {
            var data = await _dashboardService.GetDashboardDataAsync(warehouseId);
            return Ok(data);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi lấy dữ liệu dashboard", error = ex.Message });
        }
    }
}
