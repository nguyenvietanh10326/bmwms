using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Net;

namespace BMWMS.API.Controllers.Inventory;

[Route("api/[controller]")]
[ApiController]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _poService;

    public PurchaseOrdersController(IPurchaseOrderService poService)
    {
        _poService = poService;
    }

    [HttpGet("{id:long}")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF")]
    public async Task<IActionResult> GetById(long id)
    {
        try
        {
            var po = await _poService.GetOrderDetailAsync(id);
            if (po == null)
                return NotFound(new { message = $"Không tìm thấy đơn mua hàng với ID = {id}." });

            return Ok(po);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi hệ thống khi lấy chi tiết đơn mua hàng.", detail = ex.Message });
        }
    }

    [HttpGet("AllSupplier")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF")]
    public async Task<IActionResult> GetAllSupplier() => Ok(await _poService.GetLookupListAsync());

    [HttpGet("AllWarehouse")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF")]
    public async Task<IActionResult> GetAllWarehouse() => Ok(await _poService.GetLookListAsync());

    [HttpGet("AllProduct")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF")]
    public async Task<IActionResult> GetAllProduct() => Ok(await _poService.GetUpListAsync());

    [HttpPost]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF")]
    public async Task<IActionResult> Create([FromBody] PurchaseOrderCreateDto request)
    {
        try
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

            var result = await _poService.CreatePurchaseOrderAsync(request, userId);
            if (result.Success)
                return Ok(new { message = "Tạo đơn mua hàng ở trạng thái nháp thành công.", data = result.Message });

            return BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi hệ thống khi tạo đơn mua hàng.", detail = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF")]
    public async Task<IActionResult> GetPaged([FromQuery] PurchaseOrderFilterDto filter) =>
        Ok(await _poService.GetPagedOrdersAsync(filter));

    [HttpPost("{id}/confirm")]
    [Authorize(Roles = "SYSTEM_ADMIN,PURCHASING_STAFF")]
    public async Task<IActionResult> Confirm(long id)
    {
        try
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

            var result = await _poService.ConfirmOrderAsync(id, userId);
            return result.Success
                ? Ok(new { message = result.Message })
                : BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi hệ thống khi xác nhận đơn mua hàng.", detail = ex.Message });
        }
    }

    [HttpPost("{id}/send-to-supplier")]
    [Authorize(Roles = "SYSTEM_ADMIN,PURCHASING_STAFF")]
    public async Task<IActionResult> SendToSupplier(long id)
    {
        try
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

            var result = await _poService.SendOrderToSupplierAsync(id, userId);
            return result.Success
                ? Ok(new { message = result.Message })
                : BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi hệ thống khi gửi PO cho nhà cung cấp.", detail = ex.Message });
        }
    }

    [HttpGet("{id}/supplier-response")]
    [AllowAnonymous]
    public IActionResult SupplierResponsePage(long id, [FromQuery] string action, [FromQuery] string token)
    {
        var normalizedAction = action?.Trim().ToLowerInvariant();
        if (normalizedAction is not ("confirm" or "cancel") || string.IsNullOrWhiteSpace(token))
            return Content(BuildSupplierResponseHtml(false, "Liên kết phản hồi không hợp lệ."), "text/html; charset=utf-8");

        var actionLabel = normalizedAction == "confirm" ? "xác nhận" : "từ chối";
        var buttonColor = normalizedAction == "confirm" ? "#198754" : "#dc3545";
        var encodedToken = WebUtility.HtmlEncode(token);
        var html = $"""
            <!doctype html><html lang="vi"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Phản hồi PO</title></head>
            <body style="font-family:Arial,sans-serif;background:#f4f6fa;color:#172033;padding:32px">
              <main style="max-width:560px;margin:40px auto;background:#fff;border-radius:12px;padding:28px;box-shadow:0 4px 20px rgba(16,24,40,.08)">
                <h1 style="font-size:22px;margin-top:0">Phản hồi đơn đặt hàng</h1>
                <p>Bạn đang chọn <strong>{actionLabel}</strong> PO #{id}. Hệ thống sẽ kiểm tra token và trạng thái hiện tại trước khi cập nhật.</p>
                <form method="post" action="/api/PurchaseOrders/{id}/supplier-response">
                  <input type="hidden" name="action" value="{normalizedAction}">
                  <input type="hidden" name="token" value="{encodedToken}">
                  <button type="submit" style="border:0;border-radius:7px;background:{buttonColor};color:#fff;padding:11px 18px;font-weight:700;cursor:pointer">Xác nhận lựa chọn</button>
                </form>
              </main>
            </body></html>
            """;
        return Content(html, "text/html; charset=utf-8");
    }

    [HttpPost("{id}/supplier-response")]
    [AllowAnonymous]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> SubmitSupplierResponse(
        long id,
        [FromForm] string action,
        [FromForm] string token)
    {
        try
        {
            var result = await _poService.HandleSupplierResponseAsync(id, action, token);
            return Content(BuildSupplierResponseHtml(result.Success, result.Message), "text/html; charset=utf-8");
        }
        catch
        {
            return Content(BuildSupplierResponseHtml(false, "Hệ thống chưa thể ghi nhận phản hồi. Vui lòng thử lại hoặc liên hệ bộ phận mua hàng."), "text/html; charset=utf-8");
        }
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF")]
    public async Task<IActionResult> Cancel(long id, [FromQuery] string? reason)
    {
        try
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();

            var result = await _poService.CancelOrderAsync(id, userId, reason);
            return result.Success
                ? Ok(new { message = result.Message })
                : BadRequest(new { message = result.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi hệ thống khi hủy đơn mua hàng.", detail = ex.Message });
        }
    }

    [HttpPost("{id}/close-partial")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public async Task<IActionResult> ClosePartial(long id, [FromQuery] string reason)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        var result = await _poService.ClosePartiallyReceivedOrderAsync(id, userId, reason);
        return result.Success
            ? Ok(new { message = result.Message })
            : BadRequest(new { message = result.Message });
    }

    [HttpPost("{id}/continue-partial")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public async Task<IActionResult> ContinuePartial(long id)
    {
        if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
        var result = await _poService.ContinuePartiallyReceivedOrderAsync(id, userId);
        return result.Success
            ? Ok(new { message = result.Message })
            : BadRequest(new { message = result.Message });
    }

    [HttpGet("/AllCustomers")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,SALES_STAFF")]
    public async Task<IActionResult> GetAllCustomer()
    {
        try
        {
            return Ok(await _poService.GetCustomersAsync());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Lỗi khi lấy danh sách khách hàng.", detail = ex.Message });
        }
    }

    private bool TryGetCurrentUserId(out long userId) =>
        long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static string BuildSupplierResponseHtml(bool success, string message)
    {
        var title = success ? "Đã ghi nhận phản hồi" : "Không thể ghi nhận phản hồi";
        var color = success ? "#198754" : "#dc3545";
        return $"""
            <!doctype html><html lang="vi"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{title}</title></head>
            <body style="font-family:Arial,sans-serif;background:#f4f6fa;color:#172033;padding:32px">
              <main style="max-width:560px;margin:40px auto;background:#fff;border-radius:12px;padding:28px;box-shadow:0 4px 20px rgba(16,24,40,.08)">
                <h1 style="font-size:22px;color:{color};margin-top:0">{title}</h1>
                <p>{WebUtility.HtmlEncode(message)}</p>
                <p style="color:#667085">Bạn có thể đóng trang này.</p>
              </main>
            </body></html>
            """;
    }
}
