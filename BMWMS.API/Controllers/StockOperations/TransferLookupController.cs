using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using BMWMS.Business.DTOs.StockOperations;
using BMWMS.Business.Interfaces.StockOperations;

namespace BMWMS.API.Controllers.StockOperations
{
    [ApiController]
    [Route("api/transfers")]
    [Authorize]
    public class TransferLookupController : ControllerBase
    {
        private readonly ITransferLookupService _service;

        public TransferLookupController(ITransferLookupService service)
        {
            _service = service;
        }

        [HttpGet("zones")]
        public async Task<IActionResult> GetZones([FromQuery] long warehouseId = 1) => Ok(await _service.GetZonesAsync(warehouseId));

        [HttpGet("racks")]
        public async Task<IActionResult> GetRacks([FromQuery] long warehouseId = 1, [FromQuery] long? zoneId = null) => Ok(await _service.GetRacksAsync(warehouseId, zoneId));

        [HttpGet("locations")]
        public async Task<IActionResult> GetLocations([FromQuery] long warehouseId = 1, [FromQuery] long? zoneId = null, [FromQuery] long? rackId = null) => Ok(await _service.GetLocationsAsync(warehouseId, zoneId, rackId));

        [HttpGet("location-inventory")]
        public async Task<IActionResult> GetLocationInventory([FromQuery] long locationId) => Ok(await _service.GetLocationInventoryAsync(locationId));

        [HttpGet("validate-destination")]
        public async Task<IActionResult> ValidateDestination([FromQuery] long locationId, [FromQuery] long productId, [FromQuery] decimal requestedQuantity) => Ok(await _service.ValidateDestinationAsync(locationId, productId, requestedQuantity));

        [HttpGet("staff-users")]
        public async Task<IActionResult> GetStaffUsers() => Ok(await _service.GetStaffUsersAsync());

        [HttpGet("use-cases")]
        public async Task<IActionResult> GetUseCases() => Ok(await _service.GetUseCasesAsync());
    }
}
