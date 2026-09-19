using System.Security.Claims;
using BMWMS.Business.DTOs.Stocktake;
using BMWMS.Business.Interfaces.Stocktake;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers
{
    [ApiController]
    [Route("api/stocktakes")]
    [Authorize]
    public class StocktakesController : ControllerBase
    {
        private readonly IStocktakeService _stocktakeService;

        public StocktakesController(IStocktakeService stocktakeService)
        {
            _stocktakeService = stocktakeService;
        }

        [HttpGet("locations")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> GetLocations([FromQuery] long warehouseId, [FromQuery] List<long>? rackIds = null, [FromQuery] List<long>? productGroupIds = null)
        {
            if (warehouseId <= 0)
                return BadRequest(new { message = "Kho không hợp lệ." });

            var result = await _stocktakeService.GetLocationOptionsAsync(warehouseId, rackIds, productGroupIds);
            return Ok(result);
        }

        [HttpGet("staff-users")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> GetStaffUsers()
        {
            var result = await _stocktakeService.GetStaffUsersAsync();
            return Ok(result);
        }

        // UC33 - Xem danh sách phiếu kiểm kho
        [HttpGet]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> GetStocktakes([FromQuery] StocktakeFilterDto filter)
        {
            var result = await _stocktakeService.GetSessionsAsync(filter, GetCurrentUserId(), CanManage());
            return Ok(result);
        }

        // UC34 - Tạo phiếu kiểm kho
        [HttpPost]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> CreateStocktake([FromBody] CreateStocktakeSessionDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _stocktakeService.CreateSessionAsync(request, GetCurrentUserId());
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        // UC37 - Xem chi tiết phiếu kiểm kho
        [HttpGet("{id:long}")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> GetStocktake(long id)
        {
            var result = await _stocktakeService.GetSessionDetailAsync(id, GetCurrentUserId(), CanManage());
            if (result == null)
                return NotFound(new { message = "Không tìm thấy phiếu kiểm kho." });

            return Ok(result);
        }

        // UC34 - Bắt đầu phiếu kiểm kho
        [HttpPost("{id:long}/start")]
        [Authorize(Roles = "WAREHOUSE_STAFF")]
        public async Task<IActionResult> StartStocktake(long id)
        {
            var result = await _stocktakeService.StartSessionAsync(id, GetCurrentUserId());
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        // UC35 - Hủy phiếu kiểm kho
        [HttpPost("{id:long}/cancel")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> CancelStocktake(long id, [FromBody] StocktakeNoteDto? request)
        {
            var result = await _stocktakeService.CancelSessionAsync(id, GetCurrentUserId(), request?.Notes);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        // UC34 - Lay nhiem vu dem cho bin
        [HttpGet("{id:long}/locations/{locationId:long}/count-task")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> GetCountTask(long id, long locationId)
        {
            var result = await _stocktakeService.GetCountTaskAsync(id, locationId, GetCurrentUserId(), CanManage());
            if (result == null)
                return NotFound(new { message = "Không tìm thấy nhiệm vụ kiểm kho." });

            return Ok(result);
        }

        // UC34 - Luu so dem (draft)
        [HttpPut("{id:long}/locations/{locationId:long}/counts")]
        [Authorize(Roles = "WAREHOUSE_STAFF")]
        public async Task<IActionResult> SaveCounts(long id, long locationId, [FromBody] List<StocktakeCountLineDto> lines)
        {
            var result = await _stocktakeService.SaveCountsAsync(id, locationId, lines ?? new(), GetCurrentUserId(), CanManage());
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        // Nhân viên gửi một lần sau khi đã nhập xong toàn bộ phiếu.
        [HttpPost("{id:long}/submit")]
        [Authorize(Roles = "WAREHOUSE_STAFF")]
        public async Task<IActionResult> SubmitSession(long id)
        {
            var result = await _stocktakeService.SubmitSessionAsync(id, GetCurrentUserId());
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        // UC36 - Phe duyet va dieu chinh ton kho
        [HttpPost("{id:long}/approve")]
        [Authorize(Roles = "WAREHOUSE_MANAGER,SYSTEM_ADMIN")]
        public async Task<IActionResult> ApproveStocktake(long id, [FromBody] StocktakeNoteDto? request)
        {
            var result = await _stocktakeService.ApproveSessionAsync(
                id,
                GetCurrentUserId(),
                request ?? new StocktakeNoteDto());
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        [HttpPut("{id:long}/counts")]
        [Authorize(Roles = "WAREHOUSE_STAFF")]
        public async Task<IActionResult> SaveSessionCounts(long id, [FromBody] SaveStocktakeSessionCountsDto request)
        {
            var result = await _stocktakeService.SaveSessionCountsAsync(id, request ?? new(), GetCurrentUserId());
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        [HttpPost("{id:long}/reject")]
        [Authorize(Roles = "WAREHOUSE_MANAGER,SYSTEM_ADMIN")]
        public async Task<IActionResult> RejectStocktake(long id, [FromBody] StocktakeNoteDto? request)
        {
            var result = await _stocktakeService.RejectSessionAsync(id, GetCurrentUserId(), request?.Notes);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("userId")?.Value;

            return long.TryParse(claim, out var id) ? id : 1;
        }

        private bool CanManage()
        {
            return User.IsInRole("SYSTEM_ADMIN") || User.IsInRole("WAREHOUSE_MANAGER");
        }
    }
}
