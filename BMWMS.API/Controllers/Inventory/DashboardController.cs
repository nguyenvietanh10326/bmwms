using BMWMS.Business.Interfaces.Inventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers.Inventory
{
    [Route("api/dashboard")]
    [ApiController]
    public class InventoryDashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public InventoryDashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var result = await _dashboardService.GetDashboardSummaryAsync();
            return Ok(result);
        }
    }
}
