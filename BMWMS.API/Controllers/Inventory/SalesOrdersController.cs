using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BMWMS.API.Controllers.Inventory
{
    public class SalesOrdersController : ControllerBase
    {
        private readonly ISalesOrderService _salesOrderService;

        public SalesOrdersController(ISalesOrderService salesOrderService)
        {
            _salesOrderService = salesOrderService;
        }

        // Helper lấy Current User ID từ Claims
        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long userId))
            {
                return userId;
            }
            return 1; // Fallback ID mặc định (vd: Admin) nếu chưa cấu hình Identity/Claims
        }

        /// <summary>
        /// GET: api/salesorders
        /// Lấy danh sách Đơn bán hàng (có lọc & phân trang)
        /// </summary>
        [HttpGet("salesorders")]
        public async Task<IActionResult> GetPaged([FromQuery] SalesOrderSearchCriteria criteria)
        {
            try
            {
                var result = await _salesOrderService.GetPagedAsync(criteria);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// GET: api/salesorders/{id}
        /// Lấy chi tiết đơn bán hàng theo ID
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var result = await _salesOrderService.GetByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new { success = false, message = $"Không tìm thấy đơn bán hàng có ID = {id}" });
                }

                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// POST: api/salesorders
        /// Tạo mới Đơn bán hàng (Trạng thái Nháp - DRAFT)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateDraft([FromBody] CreateUpdateSalesOrderDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu đầu vào không hợp lệ!" });
            }

            try
            {
                dto.CurrentUserId = GetCurrentUserId();
                var result = await _salesOrderService.CreateDraftAsync(dto);

                return CreatedAtAction(nameof(GetById), new { id = result.SalesOrderId }, new
                {
                    success = true,
                    message = "Tạo mới đơn bán hàng thành công!",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// PUT: api/salesorders/{id}
        /// Cập nhật Đơn bán hàng (Chỉ khi ở trạng thái DRAFT)
        /// </summary>
        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateDraft(long id, [FromBody] CreateUpdateSalesOrderDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu đầu vào không hợp lệ!" });
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
                        message = "Cập nhật thất bại! Đơn hàng không tồn tại hoặc không ở trạng thái Nháp (DRAFT)."
                    });
                }

                return Ok(new { success = true, message = "Cập nhật đơn bán hàng thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// POST: api/salesorders/{id}/confirm
        /// Kiểm tra tồn kho & Xác nhận giữ tồn (Đổi trạng thái DRAFT -> ALLOCATED)
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
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// POST: api/salesorders/{id}/cancel
        /// Hủy đơn bán hàng (Đổi trạng thái -> CANCELLED)
        /// </summary>
        [HttpPost("{id:long}/cancel")]
        public async Task<IActionResult> CancelOrder(long id, [FromBody] CancelOrderRequest request)
        {
            try
            {
                long currentUserId = GetCurrentUserId();
                string reason = request?.Reason ?? "Hủy đơn từ hệ thống";

                var (isSuccess, message) = await _salesOrderService.CancelOrderAsync(id, currentUserId, reason);

                if (!isSuccess)
                {
                    return BadRequest(new { success = false, message });
                }

                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }

   
}
