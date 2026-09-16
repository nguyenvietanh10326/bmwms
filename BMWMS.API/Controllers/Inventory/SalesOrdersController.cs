using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BMWMS.API.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF,ACCOUNTANT,DIRECTOR")]
    public class SalesOrdersController : ControllerBase
    {
        private readonly ISalesOrderService _salesOrderService;
        private readonly ILogger<SalesOrdersController> _logger;

        public SalesOrdersController(ISalesOrderService salesOrderService, ILogger<SalesOrdersController> logger)
        {
            _salesOrderService = salesOrderService;
            _logger = logger;
        }

        // Helper láº¥y Current User ID tá»« Claims
        private bool TryGetCurrentUserId(out long userId)
            => long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);

        /// <summary>
        /// GET: api/salesorders
        /// Láº¥y danh sÃ¡ch ÄÆ¡n bÃ¡n hÃ ng (cÃ³ lá»c & phÃ¢n trang)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] SalesOrderSearchCriteria criteria)
        {
            try
            {
                var result = await _salesOrderService.GetPagedAsync(criteria);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        /// <summary>
        /// GET: api/salesorders/{id}
        /// Láº¥y chi tiáº¿t Ä‘Æ¡n bÃ¡n hÃ ng theo ID
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var result = await _salesOrderService.GetByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new { success = false, message = $"KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n bÃ¡n hÃ ng cÃ³ ID = {id}" });
                }

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lá»—i há»‡ thá»‘ng: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        /// <summary>
        /// GET: api/salesorders/lookups/products
        /// Danh mục sản phẩm đang hoạt động phục vụ tạo SO, không bao gồm dữ liệu giá.
        /// </summary>
        [HttpGet("lookups/products")]
        public async Task<IActionResult> GetProductLookups()
        {
            var products = await _salesOrderService.GetActiveProductLookupsAsync();
            return Ok(products);
        }

        /// <summary>
        /// POST: api/salesorders
        /// Tạo mới đơn bán hàng ở trạng thái Nháp (DRAFT)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SALES_STAFF,SYSTEM_ADMIN")]
        public async Task<IActionResult> CreateDraft([FromBody] CreateUpdateSalesOrderDto dto)
        {
            if (!ModelState.IsValid)
            {
                var message = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                    ?? "Dữ liệu đầu vào không hợp lệ.";
                return BadRequest(new { success = false, message });
            }

            try
            {
                if (!TryGetCurrentUserId(out var currentUserId))
                    return Unauthorized(new { success = false, message = "Phiên đăng nhập không hợp lệ. Vui lòng đăng nhập lại." });
                dto.CurrentUserId = currentUserId;
                var result = await _salesOrderService.CreateDraftAsync(dto);

                return CreatedAtAction(nameof(GetById), new { id = result.SalesOrderId }, new
                {
                    success = true,
                    message = "Tạo đơn bán hàng nháp thành công.",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot create sales order draft for current user {UserId}.", dto.CurrentUserId);
                return StatusCode(500, new { success = false, message = "Không thể tạo đơn bán hàng. Vui lòng thử lại." });
            }
        }

        /// <summary>
        /// PUT: api/salesorders/{id}
        /// Cáº­p nháº­t ÄÆ¡n bÃ¡n hÃ ng (Chá»‰ khi á»Ÿ tráº¡ng thÃ¡i DRAFT)
        /// </summary>
        [HttpPut("{id:long}")]
        [Authorize(Roles = "SALES_STAFF,SYSTEM_ADMIN")]
        public async Task<IActionResult> UpdateDraft(long id, [FromBody] CreateUpdateSalesOrderDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu đầu vào không hợp lệ." });
            }

            try
            {
                if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
                dto.SalesOrderId = id;
                dto.CurrentUserId = currentUserId;

                bool isUpdated = await _salesOrderService.UpdateDraftAsync(dto);
                if (!isUpdated)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Không thể cập nhật: đơn không tồn tại hoặc không còn ở trạng thái nháp."
                    });
                }

                return Ok(new { success = true, message = "Đã cập nhật đơn bán hàng." });
            }
            catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Không thể cập nhật SO. Vui lòng kiểm tra database/migration hoặc liên hệ quản trị viên." });
            }
        }

        /// <summary>
        /// POST: api/salesorders/{id}/confirm
        /// Kiá»ƒm tra tá»“n kho & XÃ¡c nháº­n giá»¯ tá»“n (Äá»•i tráº¡ng thÃ¡i DRAFT -> ALLOCATED)
        /// </summary>
        [HttpPost("{id:long}/confirm")]
        [Authorize(Roles = "WAREHOUSE_MANAGER,SYSTEM_ADMIN")]
        public async Task<IActionResult> ConfirmAndReserveStock(long id)
        {
            try
            {
                if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
                var (isSuccess, message) = await _salesOrderService.ConfirmAndReserveStockAsync(id, currentUserId);

                if (!isSuccess)
                {
                    return BadRequest(new { success = false, message });
                }

                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lá»—i há»‡ thá»‘ng: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        /// <summary>
        /// POST: api/salesorders/{id}/cancel
        /// Há»§y Ä‘Æ¡n bÃ¡n hÃ ng (Äá»•i tráº¡ng thÃ¡i -> CANCELLED)
        /// </summary>
        [HttpPost("{id:long}/cancel")]
        [Authorize(Roles = "SALES_STAFF,WAREHOUSE_MANAGER,SYSTEM_ADMIN")]
        public async Task<IActionResult> CancelOrder(long id, [FromBody] CancelOrderRequest request)
        {
            try
            {
                if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
                string reason = request?.Reason ?? "Há»§y Ä‘Æ¡n tá»« há»‡ thá»‘ng";

                var (isSuccess, message) = await _salesOrderService.CancelOrderAsync(id, currentUserId, reason);

                if (!isSuccess)
                {
                    return BadRequest(new { success = false, message });
                }

                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lá»—i há»‡ thá»‘ng: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpPost("{id:long}/reject")]
        [Authorize(Roles = "WAREHOUSE_MANAGER,SYSTEM_ADMIN")]
        public async Task<IActionResult> RejectDraft(long id, [FromBody] CancelOrderRequest request)
        {
            if (!TryGetCurrentUserId(out var userId)) return Unauthorized();
            var result = await _salesOrderService.RejectDraftAsync(id, userId, request?.Reason ?? "");
            return result.IsSuccess ? Ok(new { success = true, message = result.Message })
                : BadRequest(new { success = false, message = result.Message });
        }
    }
}
