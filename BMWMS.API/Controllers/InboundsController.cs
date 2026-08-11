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
        if (User.IsInRole("WAREHOUSE_STAFF"))
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out var currentUserId))
            {
                filter.AssignedToUserId = currentUserId;
            }
            else
            {
                return Unauthorized();
            }
        }

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

    [HttpPost]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public async Task<ActionResult<long>> CreateInboundOrder([FromBody] CreateInboundOrderDto dto)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        var newOrderId = await _inboundService.CreateInboundOrderAsync(dto, currentUserId);
        return Ok(newOrderId);
    }

    [HttpGet("purchase-orders/{poId}")]
    public async Task<ActionResult<PurchaseOrderForInboundDto>> GetPurchaseOrderForInbound(long poId)
    {
        var result = await _inboundService.GetPurchaseOrderForInboundAsync(poId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("purchase-orders/pending")]
    public async Task<ActionResult<List<SourceOrderDropdownDto>>> GetPendingPurchaseOrders()
    {
        var result = await _inboundService.GetPendingPurchaseOrdersAsync();
        return Ok(result);
    }

    [HttpGet("sales-orders/returnable")]
    public async Task<ActionResult<List<SourceOrderDropdownDto>>> GetReturnableSalesOrders()
    {
        var result = await _inboundService.GetReturnableSalesOrdersAsync();
        return Ok(result);
    }

    [HttpGet("sales-orders/{soId}")]
    public async Task<ActionResult<PurchaseOrderForInboundDto>> GetSalesOrderForInbound(long soId)
    {
        var result = await _inboundService.GetSalesOrderForInboundAsync(soId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("shortages")]
    public async Task<ActionResult<List<ShortageInboundOrderDto>>> GetShortageInboundOrders()
    {
        var result = await _inboundService.GetShortageInboundOrdersAsync();
        return Ok(result);
    }

    [HttpGet("{parentId}/supplement")]
    public async Task<ActionResult<PurchaseOrderForInboundDto>> GetInboundOrderForSupplement(long parentId)
    {
        var result = await _inboundService.GetInboundOrderForSupplementAsync(parentId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateInboundOrderDto dto)
    {
        try
        {
            var userId = 1; // Tạm hardcode
            await _inboundService.UpdateInboundOrderAsync(id, dto, userId);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelInboundOrderDto dto)
    {
        try
        {
            var userId = 1; // Tạm hardcode
            await _inboundService.CancelInboundOrderAsync(id, dto, userId);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("{id}/confirm")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public async Task<IActionResult> Confirm(long id)
    {
        try
        {
            var userId = 1; // Tạm hardcode
            await _inboundService.ConfirmInboundOrderAsync(id, userId);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("{id}/receive")]
    public async Task<ActionResult<long>> ReceiveItem(long id, [FromBody] ReceiveInboundItemDto dto)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out var currentUserId)) return Unauthorized();

        try
        {
            var lotId = await _inboundService.ReceiveItemAsync(id, dto, currentUserId);
            return Ok(new { ProductLotId = lotId });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/receive-batch")]
    public async Task<IActionResult> ReceiveBatch(long id, [FromBody] ReceiveBatchInboundDto dto)
    {
        var currentUserId = 1; // Fake logic for current user
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(userIdClaim, out var parsedId))
        {
            currentUserId = (int)parsedId;
        }

        try
        {
            await _inboundService.ReceiveBatchAsync(id, dto, currentUserId);
            return Ok(new { Message = "Đã nhận hàng thành công" });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/putaway")]
    public async Task<IActionResult> PutawayBatch(long id, [FromBody] List<PutawayInboundItemDto> dtos)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out var currentUserId)) return Unauthorized();

        try
        {
            await _inboundService.PutawayBatchAsync(id, dtos, currentUserId);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
