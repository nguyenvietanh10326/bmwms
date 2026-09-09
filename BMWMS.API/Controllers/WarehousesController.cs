using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Warehouse;
using BMWMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class WarehousesController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehousesController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        /// <summary>
        /// Lấy danh sách kho có phân trang và lọc
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<WarehouseResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPagedList([FromQuery] WarehouseFilterDto filter)
        {
            var result = await _warehouseService.GetPagedListAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Lấy thông tin chi tiết kho theo ID
        /// </summary>
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(WarehouseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(long id)
        {
            var warehouse = await _warehouseService.GetByIdAsync(id);
            if (warehouse == null)
            {
                return NotFound(new { message = $"Không tìm thấy kho có ID = {id}." });
            }

            return Ok(warehouse);
        }

        /// <summary>
        /// Tạo mới kho
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public IActionResult Create([FromBody] CreateWarehouseDto dto)
        {
            return Conflict(new { message = "Hệ thống vận hành theo mô hình một kho; không hỗ trợ tạo thêm kho." });
        }

        /// <summary>
        /// Cập nhật thông tin kho
        /// </summary>
        [HttpPut("{id:long}")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public IActionResult Update(long id, [FromBody] UpdateWarehouseDto dto)
        {
            return Conflict(new { message = "Thông tin kho là cấu hình hệ thống dùng chung và không chỉnh sửa tại module vị trí." });
        }

        /// <summary>
        /// Xóa kho
        /// </summary>
        [HttpDelete("{id:long}")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Delete(long id)
        {
            return Conflict(new { message = "Kho duy nhất là dữ liệu hệ thống và không thể xóa." });
        }
    }
}
