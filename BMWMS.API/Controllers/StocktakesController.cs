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

        // UC33 - Xem danh sach phien kiem kho
        [HttpGet]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> GetStocktakes([FromQuery] StocktakeFilterDto filter)
        {
            var result = await _stocktakeService.GetSessionsAsync(filter, GetCurrentUserId(), CanManage());
            return Ok(result);
        }

        // UC34 - Tao phien kiem kho
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

        // UC37 - Xem chi tiet phien kiem kho
        [HttpGet("{id:long}")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> GetStocktake(long id)
        {
            var result = await _stocktakeService.GetSessionDetailAsync(id, GetCurrentUserId(), CanManage());
            if (result == null)
                return NotFound(new { message = "Khong tim thay dot kiem kho." });

            return Ok(result);
        }

        // UC34 - Bat dau phien kiem kho
        [HttpPost("{id:long}/start")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> StartStocktake(long id)
        {
            var result = await _stocktakeService.StartSessionAsync(id, GetCurrentUserId());
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        // UC35 - Huy phien kiem kho
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
                return NotFound(new { message = "Khong tim thay nhiem vu kiem dem." });

            return Ok(result);
        }

        // UC34 - Luu so dem (draft)
        [HttpPut("{id:long}/locations/{locationId:long}/counts")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> SaveCounts(long id, long locationId, [FromBody] List<StocktakeCountLineDto> lines)
        {
            var result = await _stocktakeService.SaveCountsAsync(id, locationId, lines ?? new(), GetCurrentUserId(), CanManage());
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        // UC34 - Submit bin da dem
        [HttpPost("{id:long}/locations/{locationId:long}/submit")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> SubmitLocation(long id, long locationId, [FromBody] StocktakeNoteDto? request)
        {
            var result = await _stocktakeService.SubmitLocationAsync(id, locationId, GetCurrentUserId(), CanManage(), request?.Notes);
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        // UC35 - Them hang phat sinh
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

        // UC35 - Yeu cau dem lai bin
        [HttpPost("{id:long}/locations/{locationId:long}/request-recount")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> RequestRecount(long id, long locationId)
        {
            var detail = await _stocktakeService.GetSessionDetailAsync(id, GetCurrentUserId(), CanManage());
            if (detail == null)
                return NotFound(new { message = "Khong tim thay dot kiem kho." });

            var locationItems = detail.Items
                .Where(i => i.StorageLocationId == locationId && i.CountedQuantity.HasValue)
                .Select(i => new StocktakeResolutionDto
                {
                    StocktakeItemId = i.StocktakeItemId,
                    Resolution = "RECOUNT"
                })
                .ToList();

            if (!locationItems.Any())
                return BadRequest(new { message = "Bin chua co item nao duoc dem de yeu cau recount." });

            var result = await _stocktakeService.ApplyResolutionsAsync(id, locationItems, GetCurrentUserId());
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        // UC35 - Gui phien kiem kho de review
        [HttpPost("{id:long}/submit-review")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> SubmitForReview(long id)
        {
            var detail = await _stocktakeService.GetSessionDetailAsync(id, GetCurrentUserId(), CanManage());
            if (detail == null)
                return NotFound(new { message = "Khong tim thay dot kiem kho." });

            if (detail.Status != "COUNTED")
                return BadRequest(new { message = "Chi co the submit review khi dot kiem kho o trang thai COUNTED." });

            var unresolvedVariance = detail.Items
                .Where(i => i.DifferenceQuantity.HasValue && i.DifferenceQuantity.Value != 0
                            && string.IsNullOrWhiteSpace(i.Resolution))
                .Select(i => new StocktakeResolutionDto
                {
                    StocktakeItemId = i.StocktakeItemId,
                    Resolution = "ACCEPT_DIFFERENCE"
                })
                .ToList();

            var resolveResult = await _stocktakeService.ApplyResolutionsAsync(id, unresolvedVariance, GetCurrentUserId());
            if (!resolveResult.Success)
                return BadRequest(new { message = resolveResult.Message });

            return Ok(new { success = true, message = "Da gui phien kiem kho de phe duyet." });
        }

        // UC35 - Luu xu ly chenh lech (resolutions)
        [HttpPut("{id:long}/resolutions")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> ApplyResolutions(long id, [FromBody] List<StocktakeResolutionDto> resolutions)
        {
            var result = await _stocktakeService.ApplyResolutionsAsync(id, resolutions ?? new(), GetCurrentUserId());
            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(result);
        }

        // UC36 - Phe duyet va dieu chinh ton kho
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
            return User.IsInRole("SYSTEM_ADMIN") || User.IsInRole("WAREHOUSE_MANAGER");
        }
    }
}
