using System.Threading.Tasks;
using BMWMS.Business.DTOs.Inbound;
using BMWMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,PURCHASING_STAFF")]
public class InboundsController : ControllerBase
{
    private readonly IInboundService _inboundService;

    public InboundsController(IInboundService inboundService)
    {
        _inboundService = inboundService;
    }

    [HttpGet]
    public async Task<ActionResult<InboundOrderPageDto>> GetInboundOrders([FromQuery] InboundOrderFilterDto filter)
    {
        var result = await _inboundService.GetInboundOrdersPageAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InboundOrderDetailDto>> GetInboundOrderById(long id)
    {
        var result = await _inboundService.GetInboundOrderByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }
}
