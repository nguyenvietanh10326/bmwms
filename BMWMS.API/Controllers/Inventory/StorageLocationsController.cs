using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers.Inventory
{
    [Route("api/[controller]")]
    [ApiController]
    public class StorageLocationsController : ControllerBase
    {
        private readonly IStorageLocationService _locationService;

        public StorageLocationsController(IStorageLocationService locationService)
        {
            _locationService = locationService;
        }

        /// <summary>
        /// Lấy danh sách vị trí + Thông tin Kho + Phân trang
        /// GET: api/storagelocations?warehouseId=1&keyword=Z-A&pageIndex=1&pageSize=10&zoneId=1&rackId=2
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLocations([FromQuery] StorageLocationFilterDto filter)
        {
            var result = await _locationService.GetLocationsPageAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Lấy toàn bộ cây sơ đồ phân cấp Kho -> Khu vực Zone -> Dãy/Kệ Rack -> Ô Bin (Phục vụ Sơ đồ trực quan Matrix)
        /// GET: api/storagelocations/structure?warehouseId=1
        /// </summary>
        [HttpGet("structure")]
        public async Task<IActionResult> GetStructure([FromQuery] long warehouseId = 1)
        {
            var result = await _locationService.GetWarehouseStructureAsync(warehouseId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách Khu vực (Zones) của Kho
        /// GET: api/storagelocations/zones?warehouseId=1
        /// </summary>
        [HttpGet("zones")]
        public async Task<IActionResult> GetZones([FromQuery] long warehouseId = 1)
        {
            var result = await _locationService.GetZonesAsync(warehouseId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách Kệ (Racks) của Kho hoặc theo Khu vực
        /// GET: api/storagelocations/racks?warehouseId=1&zoneId=2
        /// </summary>
        [HttpGet("racks")]
        public async Task<IActionResult> GetRacks([FromQuery] long warehouseId = 1, [FromQuery] long? zoneId = null)
        {
            var result = await _locationService.GetRacksAsync(warehouseId, zoneId);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết 1 Vị trí
        /// GET: api/storagelocations/5
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _locationService.GetByIdAsync(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy vị trí" });
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết tồn kho và danh sách Lô hàng (Lots) đang lưu trữ tại Vị trí
        /// GET: api/storagelocations/5/inventory
        /// </summary>
        [HttpGet("{id}/inventory")]
        public async Task<IActionResult> GetLocationInventory(long id)
        {
            var result = await _locationService.GetLocationInventoryAsync(id);
            if (result == null) return NotFound(new { message = "Không tìm thấy vị trí lưu kho" });
            return Ok(result);
        }

        /// <summary>
        /// Tra cứu vị trí của Sản phẩm / Mã Lô trong Kho (SCR-11 / UC10)
        /// GET: api/storagelocations/search-product?warehouseId=1&keyword=gach
        /// </summary>
        [HttpGet("search-product")]
        public async Task<IActionResult> SearchProduct([FromQuery] long warehouseId = 1, [FromQuery] string keyword = "")
        {
            var result = await _locationService.SearchProductLocationsAsync(warehouseId, keyword);
            return Ok(result);
        }

        /// <summary>
        /// Thêm vị trí mới
        /// POST: api/storagelocations
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUpdateStorageLocationDto dto)
        {
            var (success, message) = await _locationService.CreateLocationAsync(dto);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        /// <summary>
        /// Cập nhật vị trí
        /// PUT: api/storagelocations/5
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] CreateUpdateStorageLocationDto dto)
        {
            dto.StorageLocationId = id;
            var (success, message) = await _locationService.UpdateLocationAsync(dto);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        /// <summary>
        /// Thêm mới Khu vực (Zone)
        /// POST: api/storagelocations/zones
        /// </summary>
        [HttpPost("zones")]
        public async Task<IActionResult> CreateZone([FromBody] CreateUpdateZoneDto dto)
        {
            var (success, message) = await _locationService.CreateZoneAsync(dto);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }

        /// <summary>
        /// Thêm mới Kệ chứa (Rack)
        /// POST: api/storagelocations/racks
        /// </summary>
        [HttpPost("racks")]
        public async Task<IActionResult> CreateRack([FromBody] CreateUpdateRackDto dto)
        {
            var (success, message) = await _locationService.CreateRackAsync(dto);
            if (!success) return BadRequest(new { message });
            return Ok(new { message });
        }
    }
}
