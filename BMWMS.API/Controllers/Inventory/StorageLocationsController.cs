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
        /// GET: api/storagelocations?warehouseId=1&keyword=Z-A&pageIndex=1&pageSize=10
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetLocations([FromQuery] StorageLocationFilterDto filter)
        {
            var result = await _locationService.GetLocationsPageAsync(filter);
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
    
}
}
