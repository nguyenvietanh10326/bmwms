using System.Threading.Tasks;
using BMWMS.Business.DTOs.Inbound;
using BMWMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,PURCHASING_STAFF,SALES_STAFF")]
public class InboundsController : ControllerBase
{
    private readonly IInboundService _inboundService;
    private readonly ILogger<InboundsController> _logger;

    public InboundsController(IInboundService inboundService, ILogger<InboundsController> logger)
    {
        _inboundService = inboundService;
        _logger = logger;
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
        else if (User.IsInRole("SALES_STAFF"))
        {
            filter.SourceType = "SALES_RETURN";
        }
        else if (User.IsInRole("PURCHASING_STAFF"))
        {
            filter.SourceType = "PURCHASE_ORDER";
        }

        var result = await _inboundService.GetInboundOrdersPageAsync(filter);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InboundOrderDetailDto>> GetInboundOrderById(long id)
    {
        var result = await _inboundService.GetInboundOrderByIdAsync(id);
        if (result == null) return NotFound();
        if (User.IsInRole("WAREHOUSE_STAFF"))
        {
            if (!long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var currentUserId))
                return Unauthorized();
            if (result.AssignedToUserId != currentUserId)
                return Forbid();
        }
        else if (User.IsInRole("SALES_STAFF") && result.SourceType != "SALES_RETURN")
        {
            return Forbid();
        }
        else if (User.IsInRole("PURCHASING_STAFF") && result.SourceType != "PURCHASE_ORDER")
        {
            return Forbid();
        }
        return Ok(result);
    }

    [HttpGet("putaway-locations")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
    public async Task<ActionResult<List<PutawayLocationDto>>> GetPutawayLocations(
        [FromQuery] long warehouseId,
        [FromQuery] long productId,
        [FromQuery] decimal putawayQuantity = 0)
    {
        return Ok(await _inboundService.GetPutawayLocationsAsync(warehouseId, productId, putawayQuantity));
    }

    [HttpPost]
    [Authorize(Roles = "WAREHOUSE_STAFF")]
    public async Task<ActionResult<long>> CreateInboundOrder([FromBody] CreateInboundOrderDto dto)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out var currentUserId))
        {
            return Unauthorized();
        }

        try
        {
            var newOrderId = await _inboundService.CreateInboundOrderAsync(dto, currentUserId);
            return Ok(newOrderId);
        }
        catch (Exception exception)
        {
            return HandleException(exception);
        }
    }

    [HttpGet("purchase-orders/{poId}")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,WAREHOUSE_STAFF")]
    public async Task<ActionResult<PurchaseOrderForInboundDto>> GetPurchaseOrderForInbound(long poId)
    {
        var result = await _inboundService.GetPurchaseOrderForInboundAsync(poId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("purchase-orders/pending")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,WAREHOUSE_STAFF")]
    public async Task<ActionResult<List<SourceOrderDropdownDto>>> GetPendingPurchaseOrders()
    {
        var result = await _inboundService.GetPendingPurchaseOrdersAsync();
        return Ok(result);
    }

    [HttpGet("purchase-orders/available")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,WAREHOUSE_STAFF")]
    public async Task<ActionResult<List<PurchaseOrderInboundSourceDto>>> GetPurchaseOrderInboundSources()
    {
        return Ok(await _inboundService.GetPurchaseOrderInboundSourcesAsync());
    }

    [HttpGet("staff/available")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,SALES_STAFF")]
    public async Task<ActionResult<List<AvailableWarehouseStaffDto>>> GetAvailableWarehouseStaff()
    {
        return Ok(await _inboundService.GetAvailableWarehouseStaffAsync());
    }

    [HttpGet("sales-orders/returnable")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF,WAREHOUSE_STAFF")]
    public async Task<ActionResult<List<SourceOrderDropdownDto>>> GetReturnableSalesOrders()
    {
        var result = await _inboundService.GetReturnableSalesOrdersAsync();
        return Ok(result);
    }

    [HttpGet("sales-orders/{soId}")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF,WAREHOUSE_STAFF")]
    public async Task<ActionResult<PurchaseOrderForInboundDto>> GetSalesOrderForInbound(long soId)
    {
        var result = await _inboundService.GetSalesOrderForInboundAsync(soId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,SALES_STAFF")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateInboundOrderDto dto)
    {
        try
        {
            if (!long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)) return Unauthorized();
            await _inboundService.UpdateInboundOrderAsync(id, dto, userId);
            return Ok();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id}/cancel")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,SALES_STAFF")]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelInboundOrderDto dto)
    {
        try
        {
            if (!long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)) return Unauthorized();
            await _inboundService.CancelInboundOrderAsync(id, dto, userId);
            return Ok();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPut("{id}/confirm")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,SALES_STAFF")]
    public async Task<IActionResult> Confirm(long id)
    {
        try
        {
            if (!long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId)) return Unauthorized();
            await _inboundService.ConfirmInboundOrderAsync(id, userId);
            return Ok();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id}/receive")]
    [Authorize(Roles = "WAREHOUSE_STAFF")]
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
            return HandleException(ex);
        }
    }

    [HttpPost("{id}/receive-batch")]
    [Authorize(Roles = "WAREHOUSE_STAFF")]
    public async Task<IActionResult> ReceiveBatch(long id, [FromBody] ReceiveBatchInboundDto dto)
    {
        long currentUserId;
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out currentUserId)) return Unauthorized();

        try
        {
            await _inboundService.ReceiveBatchAsync(id, dto, currentUserId);
            return Ok(new { Message = "Đã nhận hàng thành công" });
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id}/complete-receipt")]
    [Authorize(Roles = "WAREHOUSE_STAFF")]
    public async Task<IActionResult> CompleteReceipt(long id, [FromBody] CompleteInboundReceiptDto dto)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out var currentUserId)) return Unauthorized();

        try
        {
            await _inboundService.CompleteReceiptAsync(id, dto, currentUserId);
            return Ok(new { Message = "Đã hoàn tất kiểm nhận; phiếu sẵn sàng xếp hàng vào vị trí kho." });
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id}/putaway")]
    [Authorize(Roles = "WAREHOUSE_STAFF")]
    public async Task<IActionResult> PutawayBatch(long id, [FromBody] List<PutawayInboundItemDto> dtos)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out var currentUserId)) return Unauthorized();

        try
        {
            await _inboundService.PutawayBatchAsync(id, new PutawayBatchRequestDto { Items = dtos }, currentUserId);
            return Ok();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    [HttpPost("{id}/putaway-with-capacity")]
    [Authorize(Roles = "WAREHOUSE_STAFF")]
    public async Task<IActionResult> PutawayBatchWithCapacity(long id, [FromBody] PutawayBatchRequestDto request)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out var currentUserId)) return Unauthorized();

        try
        {
            await _inboundService.PutawayBatchAsync(id, request, currentUserId);
            return Ok();
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }

    private ObjectResult HandleException(Exception exception)
    {
        _logger.LogError(
            exception,
            "Inbound operation failed. TraceId={TraceId}, Path={Path}",
            HttpContext.TraceIdentifier,
            HttpContext.Request.Path);

        var (status, title, detail) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Không có quyền thực hiện", exception.Message),
            ArgumentException => (StatusCodes.Status422UnprocessableEntity, "Dữ liệu phiếu nhập không hợp lệ", exception.Message),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Không thể thực hiện ở trạng thái hiện tại", exception.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Dữ liệu đã thay đổi", "Phiếu vừa được cập nhật bởi một thao tác khác. Vui lòng tải lại trang và kiểm tra số liệu."),
            DbUpdateException => (StatusCodes.Status409Conflict, "Không thể lưu dữ liệu", "Dữ liệu vi phạm quy tắc toàn vẹn của hệ thống. Vui lòng tải lại phiếu; nếu lỗi lặp lại, quản trị viên cần kiểm tra migration và dữ liệu hiện có."),
            _ => (StatusCodes.Status500InternalServerError, "Lỗi xử lý phiếu nhập", "Hệ thống không thể hoàn tất yêu cầu. Vui lòng thử lại hoặc liên hệ quản trị viên.")
        };

        return Problem(statusCode: status, title: title, detail: detail);
    }
}
