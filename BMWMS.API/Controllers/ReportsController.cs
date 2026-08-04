using BMWMS.Business.DTOs.Report;
using BMWMS.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BMWMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF")]
    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventoryReport([FromQuery] InventoryReportFilterDto filter)
    {
        try
        {
            var result = await _reportService.GetInventoryReportAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("inbound")]
    public async Task<IActionResult> GetInboundReport([FromQuery] InboundReportFilterDto filter)
    {
        try
        {
            var result = await _reportService.GetInboundReportAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("outbound")]
    public async Task<IActionResult> GetOutboundReport([FromQuery] OutboundReportFilterDto filter)
    {
        try
        {
            var result = await _reportService.GetOutboundReportAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("inoutstock")]
    public async Task<IActionResult> GetInOutStockReport([FromQuery] InOutStockReportFilterDto filter)
    {
        try
        {
            var result = await _reportService.GetInOutStockReportAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
