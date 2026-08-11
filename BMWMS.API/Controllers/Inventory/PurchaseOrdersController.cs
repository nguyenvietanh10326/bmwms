using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseOrdersController : ControllerBase
    {
        private readonly IPurchaseOrderService _poService;

        public PurchaseOrdersController(IPurchaseOrderService poService)
        {
            _poService = poService;
        }

        /// <summary>
        /// 1. Lấy danh sách Purchase Orders có Phân trang, Tìm kiếm, Lọc Trạng thái & Kho
        /// GET: api/PurchaseOrders?searchTerm=SUP-001&status=Confirmed&warehouseId=1&pageIndex=1&pageSize=10
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPagedList([FromQuery] PurchaseOrderFilterDto filter)
        {
            try
            {
                var result = await _poService.GetPagedOrdersAsync(filter);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi lấy danh sách đơn mua hàng.", detail = ex.Message });
            }
        }

        /// <summary>
        /// 2. Lấy chi tiết 1 Purchase Order theo ID
        /// GET: api/PurchaseOrders/13
        /// </summary>
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var po = await _poService.GetOrderDetailAsync(id);
                if (po == null)
                {
                    return NotFound(new { message = $"Không tìm thấy đơn mua hàng với ID = {id}" });
                }
                return Ok(po);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi lấy chi tiết đơn mua hàng.", detail = ex.Message });
            }
        }

        /// <summary>
        /// 3. Tạo mới Purchase Order (Có thể Lưu nháp hoặc Xác nhận ngay)
        /// POST: api/PurchaseOrders
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // TODO: Thay giá trị 1L bên dưới bằng ID User đang đăng nhập từ Claim/Token (ví dụ: GetCurrentUserId())
                long currentUserId = 1L;

                var (success, message, orderId) = await _poService.CreateOrderAsync(dto, currentUserId);
                if (!success)
                {
                    return BadRequest(new { message });
                }

                return CreatedAtAction(nameof(GetById), new { id = orderId }, new { message, orderId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi tạo đơn mua hàng.", detail = ex.Message });
            }
        }

        /// <summary>
        /// 4. Xác nhận Đơn mua hàng (Chuyển trạng thái từ Nháp -> Đã xác nhận)
        /// PUT: api/PurchaseOrders/13/confirm
        /// </summary>
        [HttpPut("{id:long}/confirm")]
        public async Task<IActionResult> ConfirmOrder(long id)
        {
            try
            {
                long currentUserId = 1L; // Lấy từ Token/Claim nếu có
                var (success, message) = await _poService.ConfirmOrderAsync(id, currentUserId);

                if (!success)
                {
                    return BadRequest(new { message });
                }

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi xác nhận đơn mua hàng.", detail = ex.Message });
            }
        }

        /// <summary>
        /// 5. Hủy Đơn mua hàng
        /// PUT: api/PurchaseOrders/13/cancel
        /// </summary>
        [HttpPut("{id:long}/cancel")]
        public async Task<IActionResult> CancelOrder(long id, [FromBody] CancelOrderRequest request)
        {
            try
            {
                long currentUserId = 1L; // Lấy từ Token/Claim nếu có
                var (success, message) = await _poService.CancelOrderAsync(id, currentUserId, request?.Reason);

                if (!success)
                {
                    return BadRequest(new { message });
                }

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống khi hủy đơn mua hàng.", detail = ex.Message });
            }
        }
        [HttpGet("/AllSupplier")]
        public async Task<IActionResult> GetAllSupplier()
        {
            try
            {
                var result = await _poService.GetLookupListAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách nhà cung cấp.", detail = ex.Message });
            }
        }
        [HttpGet("/AllWarehouse")]
        public async Task<IActionResult> GetAllWarehouse()
        {
            try
            {
                var result = await _poService.GetLookListAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách kho.", detail = ex.Message });
            }
        }
        [HttpGet("/AllProduct")]
        public async Task<IActionResult> GetAllProduct()
        {
            try
            {
                var result = await _poService.GetUpListAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách sản phẩm.", detail = ex.Message });
            }
        }
    }

    // Helper DTO nhận lý do hủy đơn
    public class CancelOrderRequest
    {
        public string? Reason { get; set; }
    }
}
