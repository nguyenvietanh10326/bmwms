using BMWMS.Business.DTOs.Supplier;
using BMWMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;

    public SuppliersController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF")]
    [HttpGet]
    public async Task<IActionResult> GetSuppliers([FromQuery] SupplierFilterDto filter)
    {
        try
        {
            var result = await _supplierService.GetPagedListAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF")]
    [HttpGet("{supplierCode}")]
    public async Task<IActionResult> GetSupplierDetail(string supplierCode)
    {
        try
        {
            var result = await _supplierService.GetSupplierDetailAsync(supplierCode);
            if (result == null)
            {
                return NotFound();
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF")]
    [HttpPost]
    public async Task<IActionResult> CreateSupplier([FromBody] BMWMS.Business.DTOs.Supplier.SupplierCreateRequestDto dto)
    {
        try
        {
            var userId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var supplierId = await _supplierService.CreateSupplierAsync(dto, userId, ipAddress);
            return Ok(new { SupplierId = supplierId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
