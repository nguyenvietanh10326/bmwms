using BMWMS.Business.DTOs.StockOperations;
using BMWMS.Business.Interfaces.StockOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BMWMS.API.Controllers.StockOperations
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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

        [HttpGet("zones")]
        public async Task<IActionResult> GetZones([FromQuery] long warehouseId = 1)
        {
            var result = await _transferService.GetZonesAsync(warehouseId);
            return Ok(result);
        }

        [HttpGet("racks")]
        public async Task<IActionResult> GetRacks([FromQuery] long warehouseId = 1, [FromQuery] long? zoneId = null)
        {
            var result = await _transferService.GetRacksAsync(warehouseId, zoneId);
            return Ok(result);
        }

        [HttpGet("locations")]
        public async Task<IActionResult> GetLocations(
            [FromQuery] long warehouseId = 1,
            [FromQuery] long? zoneId = null,
            [FromQuery] long? rackId = null)
        {
            var result = await _transferService.GetLocationsForDropdownAsync(warehouseId, zoneId, rackId);
            return Ok(result);
        }

        [HttpGet("location-inventory")]
        public async Task<IActionResult> GetLocationInventory([FromQuery] long locationId)
        {
            if (locationId <= 0) return BadRequest(new { message = "LocationId khong hop le." });
            var result = await _transferService.GetLocationInventoryAsync(locationId);
            return Ok(result);
        }

        [HttpGet("validate-destination")]
        public async Task<IActionResult> ValidateDestination(
            [FromQuery] long destLocationId,
            [FromQuery] long sourceLocationId,
            [FromQuery] long productId)
        {
            var result = await _transferService.ValidateDestinationAsync(destLocationId, sourceLocationId, productId);
            return Ok(result);
        }

        [HttpGet("staff-users")]
        [Authorize(Roles = "WAREHOUSE_MANAGER")]
        public async Task<IActionResult> GetStaffUsers()
        {
            var result = await _transferService.GetStaffUsersAsync();
            return Ok(result);
        }

        [HttpGet("use-cases")]
        public async Task<IActionResult> GetUseCases()
        {
            var result = await _transferService.GetUseCasesAsync();
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] TransferOrderFilterDto filter)
        {
            var result = await _transferService.GetPagedOrdersAsync(filter);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(long id)
        {
            var result = await _transferService.GetOrderDetailAsync(id);
            if (result == null) return NotFound(new { message = "Khong tim thay phieu dieu chuyen." });
            return Ok(result);
        }

        [HttpPost("create")]
        [Authorize(Roles = "WAREHOUSE_MANAGER")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateTransferOrderDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = GetCurrentUserId();
            var result = await _transferService.CreatePendingOrderAsync(request, userId);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "WAREHOUSE_MANAGER")]
        public async Task<IActionResult> UpdateDraftOrder(long id, [FromBody] UpdateTransferOrderDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = GetCurrentUserId();
            var result = await _transferService.UpdateDraftOrderAsync(id, request, userId);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(result);
        }

        [HttpPost("{id}/approve")]
        [Authorize(Roles = "WAREHOUSE_MANAGER")]
        public async Task<IActionResult> ApproveOrder(long id, [FromBody] ApproveTransferDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _transferService.ApproveOrderAsync(id, userId, dto);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(result);
        }

        [HttpPost("{id}/reject")]
        [Authorize(Roles = "WAREHOUSE_MANAGER")]
        public async Task<IActionResult> RejectOrder(long id, [FromBody] ApproveTransferDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _transferService.RejectOrderAsync(id, userId, dto.Notes);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(result);
        }

        [HttpPost("{id}/issue")]
        [Authorize(Roles = "WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> ConfirmIssue(long id, [FromBody] ConfirmTransferDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _transferService.ConfirmTransferIssueAsync(id, userId, dto.Notes);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(result);
        }

        [HttpPost("{id}/receive")]
        [Authorize(Roles = "WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> ConfirmReceipt(long id, [FromBody] ConfirmTransferDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _transferService.ConfirmTransferReceiptAsync(id, userId, dto.Notes);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(result);
        }

        // Legacy one-shot confirm for existing clients.
        [HttpPost("{id}/confirm")]
        [Authorize(Roles = "WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
        public async Task<IActionResult> ConfirmTransfer(long id, [FromBody] ConfirmTransferDto dto)
        {
            var userId = GetCurrentUserId();
            var result = await _transferService.ConfirmTransferAsync(id, userId, dto.Notes);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(result);
        }
    }
}
