using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Product;
using BMWMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BMWMS.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        private long GetCurrentUserId()
        {
            var claim = User.FindFirst("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && long.TryParse(claim.Value, out var id))
            {
                return id;
            }
            return 1; // Default fallback admin if not available
        }

        /// <summary>
        /// Lấy danh sách sản phẩm có phân trang và lọc (UC11)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResultDto<ProductResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPagedList([FromQuery] ProductFilterDto filter)
        {
            var result = await _productService.GetPagedListAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách đơn vị tính (UOM)
        /// </summary>
        [HttpGet("units")]
        [ProducesResponseType(typeof(List<UnitOfMeasureDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnitsOfMeasure()
        {
            var units = await _productService.GetUnitsOfMeasureAsync();
            return Ok(units);
        }

        /// <summary>
        /// Lấy danh sách nhóm hàng hóa (Product Groups)
        /// </summary>
        [HttpGet("groups")]
        [ProducesResponseType(typeof(List<ProductGroupOptionDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProductGroups()
        {
            var groups = await _productService.GetProductGroupsAsync();
            return Ok(groups);
        }

        /// <summary>
        /// Lấy chi tiết sản phẩm theo ID (UC14)
        /// </summary>
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(ProductDetailResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(long id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound(new { message = $"Không tìm thấy sản phẩm có ID = {id}." });
            }

            return Ok(product);
        }

        /// <summary>
        /// Tạo mới sản phẩm (UC12)
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                var id = await _productService.CreateAsync(dto, currentUserId);
                return CreatedAtAction(nameof(GetById), new { id }, new { id, message = "Tạo sản phẩm mới thành công." });
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
        /// Cập nhật thông tin sản phẩm (UC13)
        /// </summary>
        [HttpPut("{id:long}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProductDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var currentUserId = GetCurrentUserId();
                await _productService.UpdateAsync(id, dto, currentUserId);
                return Ok(new { message = "Cập nhật sản phẩm thành công." });
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
        /// Chuyển đổi trạng thái ACTIVE / INACTIVE (UC15)
        /// </summary>
        [HttpPatch("{id:long}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                await _productService.ToggleStatusAsync(id, currentUserId);
                return Ok(new { message = "Thay đổi trạng thái sản phẩm thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Xóa sản phẩm
        /// </summary>
        [HttpDelete("{id:long}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                await _productService.DeleteAsync(id);
                return Ok(new { message = "Xóa sản phẩm thành công." });
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
