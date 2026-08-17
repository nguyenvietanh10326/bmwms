using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BMWMS.API.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalesOrdersController : ControllerBase
    {
        private readonly ISalesOrderService _salesOrderService;

        public SalesOrdersController(ISalesOrderService salesOrderService)
        {
            _salesOrderService = salesOrderService;
        }

        // Helper láº¥y Current User ID tá»« Claims
        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long userId))
            {
                return userId;
            }
            return 4; // Fallback ID máº·c Ä‘á»‹nh (vd: Admin) náº¿u chÆ°a cáº¥u hÃ¬nh Identity/Claims
        }

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
                dto.CurrentUserId = GetCurrentUserId();
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
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Không thể tạo đơn bán hàng. Vui lòng thử lại." });
            }
        }

        /// <summary>
        /// PUT: api/salesorders/{id}
        /// Cáº­p nháº­t ÄÆ¡n bÃ¡n hÃ ng (Chá»‰ khi á»Ÿ tráº¡ng thÃ¡i DRAFT)
        /// </summary>
        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateDraft(long id, [FromBody] CreateUpdateSalesOrderDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dá»¯ liá»‡u Ä‘áº§u vÃ o khÃ´ng há»£p lá»‡!" });
            }

            try
            {
                dto.SalesOrderId = id;
                dto.CurrentUserId = GetCurrentUserId();

                bool isUpdated = await _salesOrderService.UpdateDraftAsync(dto);
                if (!isUpdated)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cáº­p nháº­t tháº¥t báº¡i! ÄÆ¡n hÃ ng khÃ´ng tá»“n táº¡i hoáº·c khÃ´ng á»Ÿ tráº¡ng thÃ¡i NhÃ¡p (DRAFT)."
                    });
                }

                return Ok(new { success = true, message = "Cáº­p nháº­t Ä‘Æ¡n bÃ¡n hÃ ng thÃ nh cÃ´ng!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lá»—i há»‡ thá»‘ng: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        /// <summary>
        /// POST: api/salesorders/{id}/confirm
        /// Kiá»ƒm tra tá»“n kho & XÃ¡c nháº­n giá»¯ tá»“n (Äá»•i tráº¡ng thÃ¡i DRAFT -> ALLOCATED)
        /// </summary>
        [HttpPost("{id:long}/confirm")]
        public async Task<IActionResult> ConfirmAndReserveStock(long id)
        {
            try
            {
                long currentUserId = GetCurrentUserId();
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
        public async Task<IActionResult> CancelOrder(long id, [FromBody] CancelOrderRequest request)
        {
            try
            {
                long currentUserId = GetCurrentUserId();
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
    }
}
