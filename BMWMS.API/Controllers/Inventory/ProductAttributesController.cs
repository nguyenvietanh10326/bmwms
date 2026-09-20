using BMWMS.Business.DTOs.ProductAttribute;
using BMWMS.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BMWMS.API.Controllers.Inventory;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductAttributesController : ControllerBase
{
    private readonly IProductAttributeService _service;

    public ProductAttributesController(IProductAttributeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult> GetPaged([FromQuery] string? keyword, [FromQuery] string? status, [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetPagedAsync(keyword, status, pageIndex, pageSize);
        return Ok(result);
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<ProductAttributeDto>>> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductAttributeDto>> GetById(long id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public async Task<ActionResult<ProductAttributeDto>> Create(CreateProductAttributeDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.ProductAttributeId }, result);
        }
        catch (System.InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public async Task<ActionResult> Update(long id, UpdateProductAttributeDto dto)
    {
        try
        {
            await _service.UpdateAsync(id, dto);
            return NoContent();
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            return NotFound();
        }
        catch (System.InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public async Task<ActionResult> Delete(long id)
    {
        try
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
        catch (System.InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
