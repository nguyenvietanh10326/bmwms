using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using BMWMS.Business.Services.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using static BMWMS.Business.Interfaces.Inventory.ISalesOrderService;

namespace BMWMS.API.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,SALES_STAFF,PURCHASING_STAFF")]
    public class OutboundOrdersController : ControllerBase
    {
        private readonly IOutboundOrderService _outboundOrderService;
        private readonly ISalesOrderService _saleService;

        public OutboundOrdersController(IOutboundOrderService outboundOrderService,ISalesOrderService orderService)
        {
            _outboundOrderService = outboundOrderService;
            _saleService = orderService;

        }

        /// <summary>
        /// Lấy danh sách phiếu xuất kho (có lọc & phân trang)
        /// GET: api/OutboundOrders?Search=OUT&Status=DRAFT&PageIndex=1&PageSize=10
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOutboundOrders([FromQuery] OutboundOrderQueryFilter filter)
        {
            if (User.IsInRole("WAREHOUSE_STAFF"))
            {
                if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
                filter.AssignedToUserId = currentUserId;
            }
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

            if (User.IsInRole("WAREHOUSE_STAFF") &&
                (!TryGetCurrentUserId(out var currentUserId) || result.AssignedToUserId != currentUserId))
                return Forbid();

            return Ok(result);
        }

        /// <summary>
        /// Tạo mới phiếu xuất kho (Lưu nháp hoặc Gửi duyệt)
        /// POST: api/OutboundOrders
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF,PURCHASING_STAFF")]
        public async Task<IActionResult> Create([FromBody] CreateOutboundOrderRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();

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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var innerError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new
                {
                    message = "Lỗi hệ thống khi tạo phiếu xuất kho!",
                    detail = innerError // <--- Trả về lỗi thật từ SQL ở đây
                });
              //  return StatusCode(500, new { message = "Lỗi hệ thống khi tạo phiếu xuất kho!", detail = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái phiếu xuất kho (DRAFT, ASSIGNED, IN_PROGRESS, COMPLETED, CANCELLED)
        /// PATCH: api/OutboundOrders/5/status
        /// </summary>
        [HttpPatch("{id:long}/status")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
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
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
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
        /// <summary>
        /// Lấy thông tin chi tiết Sales Order & số lượng giữ tồn để tạo Lệnh Xuất
        /// GET: api/SalesOrders/101
        /// </summary>
        [HttpGet("sales-order/{id:long}")]
        public async Task<ActionResult<SalesOrderDetailApiResponse>> GetSalesOrderForOutbound(long id)
        {
            var result = await _saleService.GetSalesOrderDetailForOutboundAsync(id);

            if (result == null)
            {
                return NotFound(new { message = $"Không tìm thấy Sales Order với ID = {id}" });
            }

            return Ok(result);
        }
        [HttpGet("creators")]
        public async Task<IActionResult> GetSalesOrderCreators(
    CancellationToken cancellationToken)
        {
            var users = await _saleService.GetSalesOrderCreatorsAsync(cancellationToken);

            return Ok(users);
        }

        [HttpGet("staff")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF,PURCHASING_STAFF")]
        public async Task<ActionResult<List<UserSelectDto>>> GetWarehouseStaff()
        {
            return Ok(await _outboundOrderService.GetWarehouseStaffAsync());
        }
        [HttpGet("sales-orders")]
        public async Task<ActionResult<List<SalesOrderApiResponse>>> GetConfirmedSalesOrders()
        {
            var salesOrders = await _saleService.GetConfirmedSalesOrdersAsync();
            return Ok(salesOrders);
        }

        [HttpGet("purchase-orders/returnable")]
        public async Task<ActionResult<List<PurchaseOrderReturnOptionDto>>> GetReturnablePurchaseOrders()
        {
            return Ok(await _outboundOrderService.GetReturnablePurchaseOrdersAsync());
        }

        [HttpGet("purchase-order/{id:long}/return")]
        public async Task<ActionResult<PurchaseOrderForReturnDto>> GetPurchaseOrderForReturn(long id)
        {
            var result = await _outboundOrderService.GetPurchaseOrderForReturnAsync(id);
            return result == null
                ? NotFound(new { message = "PO không có hàng đã nhập còn có thể trả nhà cung cấp." })
                : Ok(result);
        }
        /// <summary>
        /// 4. MÀN 2: Lấy dữ liệu thực thi Pick hàng (Chi tiết Items, So sánh SL Cần/Đã Pick, Danh sách Bin/Lot khả dụng)
        /// </summary>
        [HttpGet("{id:long}/process")]
        public async Task<IActionResult> GetProcessDetail(long id)
        {
            var processData = await _outboundOrderService.GetOutboundProcessDetailAsync(id);
            if (processData == null)
            {
                return NotFound(new { message = $"Không tìm thấy lệnh xuất kho với ID = {id}" });
            }
            if (User.IsInRole("WAREHOUSE_STAFF") &&
                (!TryGetCurrentUserId(out var currentUserId) || processData.AssignedToUserId != currentUserId))
                return Forbid();

            return Ok(processData);
        }

        /// <summary>
        /// 5. MÀN 2: Thực thi Pick hàng từ Bin/Lot cụ thể
        /// </summary>
        [HttpPost("execute-pick")]
        [Authorize(Roles = "WAREHOUSE_STAFF")]
        public async Task<IActionResult> ExecutePick([FromBody] ExecutePickItemRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                if (!TryGetCurrentUserId(out var currentUserId)) return Unauthorized();
                var (success, message) = await _outboundOrderService.ExecutePickAsync(request, currentUserId);

                if (!success)
                {
                    return BadRequest(new { message });
                }

                return Ok(new { success = true, message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xử lý Pick hàng!", detail = ex.Message });
            }
        }
        /// <summary>
        /// 6. Cập nhật trạng thái thủ công (ASSIGNED, IN_PROGRESS, COMPLETED, CANCELLED)
        /// </summary>
        [HttpPut("{id:long}/status")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateOutboundOrderStatusRequest request)
        {
            if (id != request.OutboundOrderId)
            {
                return BadRequest(new { message = "Mã ID không trùng khớp!" });
            }

            var success = await _outboundOrderService.UpdateStatusAsync(id, request.Status);
            if (!success)
            {
                return NotFound(new { message = "Không tìm thấy lệnh xuất kho để cập nhật trạng thái!" });
            }

            return Ok(new { success = true, message = "Cập nhật trạng thái thành công!" });
        }

        /// <summary>
        /// 7. Hủy lệnh xuất kho
        /// </summary>
        [HttpPut("{id:long}/cancel")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> CancelOrder(long id)
        {
            try
            {
                var success = await _outboundOrderService.CancelOutboundOrderAsync(id);
                if (!success)
                {
                    return NotFound(new { message = "Không tìm thấy lệnh xuất kho để hủy!" });
                }

                return Ok(new { success = true, message = "Đã hủy lệnh xuất kho thành công!" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        #region Helper Methods
        private bool TryGetCurrentUserId(out long userId) =>
            long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        #endregion
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
