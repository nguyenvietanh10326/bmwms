using BMWMS.Business.DTOs.StockOperations;
using BMWMS.Business.Interfaces.StockOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BMWMS.API.Controllers.StockOperations
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransfersController : ControllerBase
    {
        private readonly ITransferService _transferService;

        public TransfersController(ITransferService transferService)
        {
            _transferService = transferService;
        }

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value
                     ?? User.FindFirst("userId")?.Value;
            return long.TryParse(claim, out var id) ? id : 1;
        }

        /// <summary>
        /// Lấy danh sách phiếu điều chuyển có phân trang & tìm kiếm.
        /// GET: api/transfers?keyword=BT&status=PENDING&warehouseId=1&pageIndex=1&pageSize=15
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] TransferOrderFilterDto filter)
        {
            var result = await _transferService.GetPagedOrdersAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết 1 phiếu điều chuyển.
        /// GET: api/transfers/5
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(long id)
        {
            var result = await _transferService.GetOrderDetailAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy phiếu điều chuyển." });

            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách hàng tồn trong ô nguồn.
        /// GET: api/transfers/location-inventory?locationId=5
        /// </summary>
        [HttpGet("location-inventory")]
        public async Task<IActionResult> GetLocationInventory([FromQuery] long locationId)
        {
            if (locationId <= 0)
                return BadRequest(new { message = "LocationId không hợp lệ." });

            var result = await _transferService.GetLocationInventoryAsync(locationId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách ô kho cho dropdown.
        /// GET: api/transfers/locations?warehouseId=1
        /// </summary>
        [HttpGet("locations")]
        public async Task<IActionResult> GetLocations([FromQuery] long warehouseId = 1)
        {
            var result = await _transferService.GetLocationsForDropdownAsync(warehouseId);
            return Ok(result);
        }

        /// <summary>
        /// Validate ô đích trước khi chuyển.
        /// GET: api/transfers/validate-destination?destLocationId=5&sourceLocationId=3&productId=10
        /// </summary>
        [HttpGet("validate-destination")]
        public async Task<IActionResult> ValidateDestination(
            [FromQuery] long destLocationId,
            [FromQuery] long sourceLocationId,
            [FromQuery] long productId)
        {
            var result = await _transferService.ValidateDestinationAsync(destLocationId, sourceLocationId, productId);
            return Ok(result);
        }

        /// <summary>
        /// [Staff] Tạo phiếu yêu cầu điều chuyển (Status = PENDING).
        /// POST: api/transfers/create
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateTransferOrderDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var result = await _transferService.CreatePendingOrderAsync(request, userId);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        /// <summary>
        /// [Manager] Phê duyệt phiếu điều chuyển (thực thi cộng trừ tồn kho).
        /// POST: api/transfers/5/approve
        /// </summary>
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveOrder(long id, [FromBody] ApproveTransferDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _transferService.ApproveOrderAsync(id, userId, dto.Notes);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        /// <summary>
        /// [Manager] Từ chối phiếu điều chuyển.
        /// POST: api/transfers/5/reject
        /// </summary>
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectOrder(long id, [FromBody] ApproveTransferDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _transferService.RejectOrderAsync(id, userId, dto.Notes);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }
    }
}
