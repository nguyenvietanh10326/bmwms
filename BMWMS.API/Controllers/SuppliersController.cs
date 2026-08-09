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

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,WAREHOUSE_STAFF,SALES_STAFF")]
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

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,WAREHOUSE_STAFF,SALES_STAFF")]
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

    [Authorize(Roles = "SYSTEM_ADMIN,PURCHASING_STAFF")]
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

    [Authorize(Roles = "SYSTEM_ADMIN,PURCHASING_STAFF")]
    [HttpPut("{supplierCode}")]
    public async Task<IActionResult> UpdateSupplier(string supplierCode, [FromBody] BMWMS.Business.DTOs.Supplier.SupplierUpdateRequestDto dto)
    {
        try
        {
            var userId = long.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _supplierService.UpdateSupplierAsync(supplierCode, dto, userId, ipAddress);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,WAREHOUSE_STAFF,SALES_STAFF")]
    [HttpGet("{supplierCode}/products")]
    public async Task<IActionResult> GetSupplierProducts(string supplierCode, [FromQuery] SupplierProductFilterDto filter)
    {
        try
        {
            var result = await _supplierService.GetSupplierProductsAsync(supplierCode, filter);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,WAREHOUSE_STAFF")]
    [HttpGet("{supplierCode}/inbound-history")]
    public async Task<IActionResult> GetSupplierInboundHistory(string supplierCode, [FromQuery] SupplierInboundHistoryFilterDto filter)
    {
        try
        {
            var result = await _supplierService.GetSupplierInboundHistoryAsync(supplierCode, filter);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,WAREHOUSE_STAFF")]
    [HttpGet("{supplierCode}/inbound-history/{inboundOrderNumber}")]
    public async Task<IActionResult> GetSupplierInboundHistoryDetail(string supplierCode, string inboundOrderNumber)
    {
        try
        {
            var detail = await _supplierService.GetSupplierInboundHistoryDetailAsync(supplierCode, inboundOrderNumber);
            if (detail == null) return NotFound("Inbound order not found or does not belong to this supplier.");
            return Ok(detail);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
