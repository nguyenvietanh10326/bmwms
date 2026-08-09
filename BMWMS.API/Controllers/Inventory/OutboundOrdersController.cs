using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BMWMS.API.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class OutboundOrdersController : ControllerBase
    {
        private readonly IOutboundOrderService _outboundOrderService;

        public OutboundOrdersController(IOutboundOrderService outboundOrderService)
        {
            _outboundOrderService = outboundOrderService;
        }

        /// <summary>
        /// Lấy danh sách phiếu xuất kho (có lọc & phân trang)
        /// GET: api/OutboundOrders?Search=OUT&Status=DRAFT&PageIndex=1&PageSize=10
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOutboundOrders([FromQuery] OutboundOrderQueryFilter filter)
        {
            var result = await _outboundOrderService.GetOutboundOrdersAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết phiếu xuất kho theo ID
        /// GET: api/OutboundOrders/5
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _outboundOrderService.GetOutboundOrderByIdAsync(id);
            if (result == null)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu xuất kho với ID = {id}" });
            }

            return Ok(result);
        }

        /// <summary>
        /// Tạo mới phiếu xuất kho (Lưu nháp hoặc Gửi duyệt)
        /// POST: api/OutboundOrders
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOutboundOrderRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                long currentUserId = GetCurrentUserId();

                var createdOrder = await _outboundOrderService.CreateOutboundOrderAsync(request, currentUserId);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = createdOrder.OutboundOrderId },
                    createdOrder
                );
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tạo phiếu xuất kho!", detail = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái phiếu xuất kho (DRAFT, ASSIGNED, IN_PROGRESS, COMPLETED, CANCELLED)
        /// PATCH: api/OutboundOrders/5/status
        /// </summary>
        [HttpPatch("{id:long}/status")]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateStatusRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new { message = "Trạng thái mới không được để trống!" });
            }

            var success = await _outboundOrderService.UpdateStatusAsync(id, request.Status.ToUpper());
            if (!success)
            {
                return NotFound(new { message = $"Không tìm thấy phiếu xuất kho với ID = {id}" });
            }

            return Ok(new { message = "Cập nhật trạng thái thành công!", status = request.Status.ToUpper() });
        }

        /// <summary>
        /// Hủy phiếu xuất kho
        /// POST: api/OutboundOrders/5/cancel
        /// </summary>
        [HttpPost("{id:long}/cancel")]
        public async Task<IActionResult> Cancel(long id)
        {
            try
            {
                var success = await _outboundOrderService.CancelOutboundOrderAsync(id);
                if (!success)
                {
                    return NotFound(new { message = $"Không tìm thấy phiếu xuất kho với ID = {id}" });
                }

                return Ok(new { message = "Hủy phiếu xuất kho thành công!" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #region Helper Methods
        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long userId))
            {
                return userId;
            }
            return 1; 
        }
        #endregion
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
