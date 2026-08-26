using BMWMS.Business.DTOs.ProductGroup;
using BMWMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
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

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && long.TryParse(claim.Value, out var id)) return id;
            return 1;
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
                return NotFound(new { message = $"Khong tim thay nhom san pham voi ID = {id}" });
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
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var userId = GetCurrentUserId();
                var id = await _productGroupService.CreateAsync(dto, userId);
                return CreatedAtAction(nameof(GetById), new { id }, new { id, message = "Tao nhom san pham thanh cong." });
            }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProductGroupDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var userId = GetCurrentUserId();
                await _productGroupService.UpdateAsync(id, dto, userId);
                return Ok(new { message = "Cap nhat nhom san pham thanh cong." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPatch("{id:long}/status")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _productGroupService.ToggleStatusAsync(id, userId);
                return Ok(new { message = "Doi trang thai nhom san pham thanh cong." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _productGroupService.DeleteAsync(id, userId);
                return Ok(new { message = "Xoa nhom san pham thanh cong." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPost("{id:long}/attributes")]
        [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
        public async Task<IActionResult> UpdateGroupAttributes(long id, [FromBody] List<GroupAttributeAssignmentDto> attributes)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _productGroupService.UpdateGroupAttributesAsync(id, attributes, userId);
                return Ok(new { message = "Cap nhat cau hinh thuoc tinh cho nhom thanh cong." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }
    }
}
