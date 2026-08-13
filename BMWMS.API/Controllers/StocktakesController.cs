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

        [HttpGet("use-cases")]
        public async Task<IActionResult> GetUseCases()
        {
            var result = await _stocktakeService.GetUseCasesAsync();
            return Ok(result);
        }

        [HttpGet("locations")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> GetLocations([FromQuery] long warehouseId)
        {
            if (warehouseId <= 0)
                return BadRequest(new { message = "WarehouseId khong hop le." });

            var result = await _stocktakeService.GetLocationOptionsAsync(warehouseId);
            return Ok(result);
        }

        [HttpGet("staff-users")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> GetStaffUsers()
        {
            var result = await _stocktakeService.GetStaffUsersAsync();
            return Ok(result);
        }

        [HttpGet("product-lots")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> SearchProductLots([FromQuery] string? keyword, [FromQuery] int take = 20)
        {
            var result = await _stocktakeService.SearchProductLotsAsync(keyword, take);
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> GetStocktakes([FromQuery] StocktakeFilterDto filter)
        {
            var result = await _stocktakeService.GetSessionsAsync(filter, GetCurrentUserId(), CanManage());
            return Ok(result);
        }

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

        [HttpGet("{id:long}")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> GetStocktake(long id)
        {
            var result = await _stocktakeService.GetSessionDetailAsync(id, GetCurrentUserId(), CanManage());
            if (result == null)
                return NotFound(new { message = "Khong tim thay dot kiem kho." });

            return Ok(result);
        }

        [HttpPost("{id:long}/start")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> StartStocktake(long id)
        {
            var result = await _stocktakeService.StartSessionAsync(id, GetCurrentUserId());
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        [HttpPost("{id:long}/cancel")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> CancelStocktake(long id, [FromBody] StocktakeNoteDto? request)
        {
            var result = await _stocktakeService.CancelSessionAsync(id, GetCurrentUserId(), request?.Notes);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        [HttpGet("{id:long}/locations/{locationId:long}/count-task")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> GetCountTask(long id, long locationId)
        {
            var result = await _stocktakeService.GetCountTaskAsync(id, locationId, GetCurrentUserId(), CanManage());
            if (result == null)
                return NotFound(new { message = "Khong tim thay nhiem vu kiem dem." });

            return Ok(result);
        }

        [HttpPut("{id:long}/locations/{locationId:long}/counts")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> SaveCounts(long id, long locationId, [FromBody] List<StocktakeCountLineDto> lines)
        {
            var result = await _stocktakeService.SaveCountsAsync(id, locationId, lines ?? new(), GetCurrentUserId(), CanManage());
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        [HttpPost("{id:long}/locations/{locationId:long}/submit")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> SubmitLocation(long id, long locationId, [FromBody] StocktakeNoteDto? request)
        {
            var result = await _stocktakeService.SubmitLocationAsync(id, locationId, GetCurrentUserId(), CanManage(), request?.Notes);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        [HttpPost("{id:long}/unexpected-items")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> AddUnexpectedItem(long id, [FromBody] UnexpectedStocktakeItemDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _stocktakeService.AddUnexpectedItemAsync(id, request, GetCurrentUserId(), CanManage());
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        [HttpPut("{id:long}/resolutions")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> ApplyResolutions(long id, [FromBody] List<StocktakeResolutionDto> resolutions)
        {
            var result = await _stocktakeService.ApplyResolutionsAsync(id, resolutions ?? new(), GetCurrentUserId());
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        [HttpPost("{id:long}/approve")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> ApproveStocktake(long id, [FromBody] StocktakeNoteDto? request)
        {
            var result = await _stocktakeService.ApproveSessionAsync(id, GetCurrentUserId(), request?.Notes);
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
            var roleCode = User.FindFirst(ClaimTypes.Role)?.Value?.ToUpperInvariant() ?? string.Empty;
            return roleCode == "SYSTEM_ADMIN" || roleCode == "WAREHOUSE_MANAGER";
        }
    }
}
