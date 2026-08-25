using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesOrdersController : ControllerBase
    {
        private readonly ISalesOrderService _soService;

        public SalesOrdersController(ISalesOrderService soService)
        {
            _soService = soService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPagedList([FromQuery] SalesOrderFilterDto filter)
        {
            var result = await _soService.GetPagedOrdersAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(long id)
        {
            var detail = await _soService.GetOrderDetailAsync(id);
            if (detail == null) return NotFound(new { message = "Không tìm thấy đơn bán hàng." });
            return Ok(detail);
        }
    }
}
