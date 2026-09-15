using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BMWMS.API.Controllers.Inventory;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,SALES_STAFF,PURCHASING_STAFF")]
public class OutboundOrdersController : ControllerBase
{
    private readonly IOutboundOrderService _outboundOrderService;
    private readonly ISalesOrderService _salesOrderService;

    public OutboundOrdersController(IOutboundOrderService outboundOrderService, ISalesOrderService salesOrderService)
    {
        _outboundOrderService = outboundOrderService;
        _salesOrderService = salesOrderService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOutboundOrders([FromQuery] OutboundOrderQueryFilter filter)
    {
        if (User.IsInRole("WAREHOUSE_STAFF"))
        {
            if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
            filter.AssignedToUserId = currentUserId;
        }
        else if (User.IsInRole("SALES_STAFF")) filter.SourceType = "SALES_ORDER";
        else if (User.IsInRole("PURCHASING_STAFF")) filter.SourceType = "PURCHASE_RETURN";

        return Ok(await _outboundOrderService.GetOutboundOrdersAsync(filter));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _outboundOrderService.GetOutboundOrderByIdAsync(id);
        if (result == null) return NotFound(new { message = "Kh+¶ng t+ºm thﬂ¶—y phiﬂ¶+u xuﬂ¶—t kho." });
        if (!CanAccessOrder(result)) return Forbid();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF,PURCHASING_STAFF")]
    public async Task<IActionResult> Create([FromBody] CreateOutboundOrderRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
        try
        {
            var createdOrder = await _outboundOrderService.CreateOutboundOrderAsync(request, currentUserId);
            return CreatedAtAction(nameof(GetById), new { id = createdOrder.OutboundOrderId }, createdOrder);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch { return StatusCode(500, new { message = "Kh+¶ng thﬂ+‚ tﬂ¶Ìo phiﬂ¶+u xuﬂ¶—t. Vui l+¶ng thﬂ+° lﬂ¶Ìi hoﬂ¶+c kiﬂ+‚m tra dﬂ+ª liﬂ+Áu hﬂ+Á thﬂ+Êng." }); }
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF,PURCHASING_STAFF")]
    public async Task<IActionResult> UpdateDraft(long id, [FromBody] UpdateOutboundOrderRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
        var (success, message) = await _outboundOrderService.UpdateDraftAsync(id, request, currentUserId);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpPost("{id:long}/approve")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public async Task<IActionResult> ApproveAndAssign(long id, [FromBody] ApproveOutboundOrderRequest request)
    {
        if (request.AssignedToUserId <= 0) return BadRequest(new { message = "Phﬂ¶˙i chﬂ+Ïn nh+Ûn vi+¨n kho -Êﬂ+‚ ph+Ûn c+¶ng." });
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
        var (success, message) = await _outboundOrderService.ApproveAndAssignAsync(id, request.AssignedToUserId, currentUserId);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpPost("{id:long}/start")]
    [Authorize(Roles = "WAREHOUSE_STAFF")]
    public async Task<IActionResult> Start(long id)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
        var (success, message) = await _outboundOrderService.StartProcessingAsync(id, currentUserId);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpPost("{id:long}/cancel")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF,PURCHASING_STAFF")]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelOutboundOrderRequest request)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
        var (success, message) = await _outboundOrderService.CancelOutboundOrderAsync(id, currentUserId, request.Reason ?? string.Empty);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpGet("sales-order/{id:long}")]
    [Authorize(Roles = "SALES_STAFF")]
    public async Task<ActionResult<SalesOrderDetailApiResponse>> GetSalesOrderForOutbound(long id)
    {
        var result = await _salesOrderService.GetSalesOrderDetailForOutboundAsync(id);
        return result == null ? NotFound(new { message = "Kh+¶ng t+ºm thﬂ¶—y SO c+¶ thﬂ+‚ tﬂ¶Ìo phiﬂ¶+u xuﬂ¶—t." }) : Ok(result);
    }

    [HttpGet("sales-orders")]
    [Authorize(Roles = "SALES_STAFF")]
    public async Task<ActionResult<List<SalesOrderApiResponse>>> GetConfirmedSalesOrders()
        => Ok(await _salesOrderService.GetConfirmedSalesOrdersAsync());

    [HttpGet("purchase-orders/returnable")]
    [Authorize(Roles = "PURCHASING_STAFF")]
    public async Task<ActionResult<List<PurchaseOrderReturnOptionDto>>> GetReturnablePurchaseOrders()
        => Ok(await _outboundOrderService.GetReturnablePurchaseOrdersAsync());

    [HttpGet("purchase-order/{id:long}/return")]
    [Authorize(Roles = "PURCHASING_STAFF")]
    public async Task<ActionResult<PurchaseOrderForReturnDto>> GetPurchaseOrderForReturn(long id)
    {
        var result = await _outboundOrderService.GetPurchaseOrderForReturnAsync(id);
        return result == null
            ? NotFound(new { message = "PO kh+¶ng c+¶n h+·ng -Ê+˙ nhﬂ¶°n c+¶ thﬂ+‚ trﬂ¶˙ nh+· cung cﬂ¶—p." })
            : Ok(result);
    }

    [HttpGet("staff")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public async Task<ActionResult<List<UserSelectDto>>> GetWarehouseStaff()
        => Ok(await _outboundOrderService.GetWarehouseStaffAsync());

    [HttpGet("{id:long}/process")]
    [Authorize(Roles = "WAREHOUSE_STAFF")]
    public async Task<IActionResult> GetProcessDetail(long id)
    {
        var processData = await _outboundOrderService.GetOutboundProcessDetailAsync(id);
        if (processData == null) return NotFound(new { message = "Kh+¶ng t+ºm thﬂ¶—y phiﬂ¶+u xuﬂ¶—t kho." });
        if (!TryGetCurrentUserId(out var currentUserId) || processData.AssignedToUserId != currentUserId) return Forbid();
        return Ok(processData);
    }

    [HttpPost("execute-pick-batch")]
    [Authorize(Roles = "WAREHOUSE_STAFF")]
    public async Task<IActionResult> ExecutePickBatch([FromBody] List<ExecutePickItemRequest> requests)
    {
        if (requests == null || requests.Count == 0) return BadRequest(new { message = "Phﬂ¶˙i chﬂ+Ïn +°t nhﬂ¶—t mﬂ+÷t d+¶ng lﬂ¶—y h+·ng." });
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
        var (success, message) = await _outboundOrderService.ExecutePickBatchAsync(requests, currentUserId);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpPost("{id:long}/complete")]
    [Authorize(Roles = "WAREHOUSE_STAFF")]
    public async Task<IActionResult> Complete(long id, [FromBody] CompleteOutboundOrderRequest request)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
        var (success, message) = await _outboundOrderService.CompleteOutboundAsync(id, currentUserId, request.Reason ?? string.Empty);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpPost("{id:long}/close-sales-remainder")]
    [Authorize(Roles = "SALES_STAFF")]
    public async Task<IActionResult> CloseSalesRemainder(long id, [FromBody] CompleteOutboundOrderRequest request)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
        var (success, message) = await _outboundOrderService.CloseRemainingSalesDemandAsync(id, currentUserId, request.Reason ?? string.Empty);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    private bool CanAccessOrder(OutboundOrderDetailDto order)
    {
        if (User.IsInRole("SYSTEM_ADMIN") || User.IsInRole("WAREHOUSE_MANAGER")) return true;
        if (User.IsInRole("WAREHOUSE_STAFF"))
            return TryGetCurrentUserId(out var currentUserId) && order.AssignedToUserId == currentUserId;
        return (User.IsInRole("SALES_STAFF") && order.SourceType == "SALES_ORDER") ||
               (User.IsInRole("PURCHASING_STAFF") && order.SourceType == "PURCHASE_RETURN");
    }

    private bool TryGetCurrentUserId(out long userId)
        => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
