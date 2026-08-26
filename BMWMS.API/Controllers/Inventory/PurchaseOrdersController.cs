using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
}
