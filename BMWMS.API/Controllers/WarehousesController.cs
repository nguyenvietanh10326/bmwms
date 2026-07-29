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
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateWarehouseDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var id = await _warehouseService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id }, new { id, message = "Tạo kho mới thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật thông tin kho
        /// </summary>
        [HttpPut("{id:long}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateWarehouseDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _warehouseService.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật thông tin kho thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Xóa kho
        /// </summary>
        [HttpDelete("{id:long}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                await _warehouseService.DeleteAsync(id);
                return Ok(new { message = "Xóa kho thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống.", detail = ex.Message });
            }
        }
    }
}
