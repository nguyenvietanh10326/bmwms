using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BMWMS.API.Controllers.Inventory;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,SALES_STAFF,PURCHASING_STAFF,ACCOUNTANT,DIRECTOR")]
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
        if (!CanReadAllOrders && User.IsInRole("WAREHOUSE_STAFF"))
        {
            if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
            filter.AssignedToUserId = currentUserId;
        }
        else if (!CanReadAllOrders && User.IsInRole("SALES_STAFF")) filter.SourceType = "SALES_ORDER";
        else if (!CanReadAllOrders && User.IsInRole("PURCHASING_STAFF")) filter.SourceType = "PURCHASE_RETURN";

        return Ok(await _outboundOrderService.GetOutboundOrdersAsync(filter));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var result = await _outboundOrderService.GetOutboundOrderByIdAsync(id);
        if (result == null) return NotFound(new { message = "Không tìm thấy phiếu xuất kho." });
        if (!CanAccessOrder(result)) return Forbid();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "WAREHOUSE_STAFF,WAREHOUSE_MANAGER,SYSTEM_ADMIN")]
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
        catch { return StatusCode(500, new { message = "Không thể tạo phiếu xuất. Vui lòng thử lại hoặc kiểm tra dữ liệu hệ thống." }); }
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "SYSTEM_ADMIN")]
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
        if (request.AssignedToUserId <= 0) return BadRequest(new { message = "Phải chọn nhân viên kho để phân công." });
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
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelOutboundOrderRequest request)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
        var (success, message) = await _outboundOrderService.CancelOutboundOrderAsync(id, currentUserId, request.Reason ?? string.Empty);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpGet("sales-order/{id:long}")]
    [Authorize(Roles = "WAREHOUSE_STAFF")]
    public async Task<ActionResult<SalesOrderDetailApiResponse>> GetSalesOrderForOutbound(long id)
    {
        var result = await _salesOrderService.GetSalesOrderDetailForOutboundAsync(id);
        return result == null ? NotFound(new { message = "Không tìm thấy SO đủ điều kiện tạo phiếu xuất." }) : Ok(result);
    }

    [HttpGet("sales-orders")]
    [Authorize(Roles = "WAREHOUSE_STAFF")]
    public async Task<ActionResult<List<SalesOrderApiResponse>>> GetConfirmedSalesOrders()
        => Ok(await _salesOrderService.GetConfirmedSalesOrdersAsync());

    [HttpGet("purchase-orders/returnable")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public async Task<ActionResult<List<PurchaseOrderReturnOptionDto>>> GetReturnablePurchaseOrders()
        => Ok(await _outboundOrderService.GetReturnablePurchaseOrdersAsync());

    [HttpGet("purchase-order/{id:long}/return")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public async Task<ActionResult<PurchaseOrderForReturnDto>> GetPurchaseOrderForReturn(long id)
    {
        var result = await _outboundOrderService.GetPurchaseOrderForReturnAsync(id);
        return result == null
            ? NotFound(new { message = "PO không còn hàng đã nhận có thể trả nhà cung cấp." })
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
        if (processData == null) return NotFound(new { message = "Không tìm thấy phiếu xuất kho." });
        if (!TryGetCurrentUserId(out var currentUserId) || processData.AssignedToUserId != currentUserId) return Forbid();
        return Ok(processData);
    }

    [HttpPost("execute-pick-batch")]
    [Authorize(Roles = "WAREHOUSE_STAFF")]
    public async Task<IActionResult> ExecutePickBatch([FromBody] List<ExecutePickItemRequest> requests)
    {
        if (requests == null || requests.Count == 0) return BadRequest(new { message = "Phải chọn ít nhất một dòng lấy hàng." });
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
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public async Task<IActionResult> CloseSalesRemainder(long id, [FromBody] CompleteOutboundOrderRequest request)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
        var (success, message) = await _outboundOrderService.CloseRemainingSalesDemandAsync(id, currentUserId, request.Reason ?? string.Empty);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    [HttpPost("{id:long}/review-completion")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public async Task<IActionResult> ReviewCompletion(long id, [FromBody] ReviewOutboundCompletionRequest request)
    {
        if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
        var (success, message) = await _outboundOrderService.ReviewCompletionAsync(id, currentUserId, request);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }

    private bool CanReadAllOrders => User.IsInRole("SYSTEM_ADMIN") || User.IsInRole("WAREHOUSE_MANAGER") || User.IsInRole("ACCOUNTANT") || User.IsInRole("DIRECTOR");

    private bool CanAccessOrder(OutboundOrderDetailDto order)
    {
        if (CanReadAllOrders) return true;
        if (User.IsInRole("WAREHOUSE_STAFF"))
            return TryGetCurrentUserId(out var currentUserId) && order.AssignedToUserId == currentUserId;
        return (User.IsInRole("SALES_STAFF") && order.SourceType == "SALES_ORDER") ||
               (User.IsInRole("PURCHASING_STAFF") && order.SourceType == "PURCHASE_RETURN");
    }

    private bool TryGetCurrentUserId(out long userId)
        => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}
