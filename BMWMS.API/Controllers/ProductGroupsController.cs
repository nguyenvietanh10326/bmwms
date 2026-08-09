using BMWMS.Business.DTOs.ProductGroup;
using BMWMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/product-groups")]
    [Route("api/categories")]
    public class ProductGroupsController : ControllerBase
    {
        private readonly IProductGroupService _productGroupService;

        public ProductGroupsController(IProductGroupService productGroupService)
        {
            _productGroupService = productGroupService;
        }

        [HttpGet]
        public async Task<IActionResult> GetPagedList([FromQuery] ProductGroupFilterDto filter)
        {
            var result = await _productGroupService.GetPagedListAsync(filter);
            return Ok(result);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetAllActive()
        {
            var result = await _productGroupService.GetAllActiveAsync();
            return Ok(result);
        }

        [HttpGet("attributes/all")]
        public async Task<IActionResult> GetAllAttributes()
        {
            var result = await _productGroupService.GetAllAttributesAsync();
            return Ok(result);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _productGroupService.GetByIdAsync(id);
            if (result == null)
            {
                return NotFound(new { message = $"Không tìm thấy nhóm sản phẩm với ID = {id}" });
            }
            return Ok(result);
        }

        [HttpGet("{id:long}/attributes")]
        public async Task<IActionResult> GetGroupAttributes(long id)
        {
            var result = await _productGroupService.GetAttributesByGroupIdAsync(id);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> Create([FromBody] CreateProductGroupDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var id = await _productGroupService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id }, new { id, message = "Tạo nhóm sản phẩm thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProductGroupDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _productGroupService.UpdateAsync(id, dto);
                return Ok(new { message = "Cập nhật nhóm sản phẩm thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("{id:long}/status")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            try
            {
                await _productGroupService.ToggleStatusAsync(id);
                return Ok(new { message = "Đổi trạng thái nhóm sản phẩm thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                await _productGroupService.DeleteAsync(id);
                return Ok(new { message = "Xóa nhóm sản phẩm thành công." });
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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:long}/attributes")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> UpdateGroupAttributes(long id, [FromBody] List<GroupAttributeAssignmentDto> attributes)
        {
            try
            {
                await _productGroupService.UpdateGroupAttributesAsync(id, attributes);
                return Ok(new { message = "Cập nhật cấu hình thuộc tính cho nhóm thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
