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
    private readonly IExcelExportService _excelExportService;

    public ReportsController(IReportService reportService, IExcelExportService excelExportService)
    {
        _reportService = reportService;
        _excelExportService = excelExportService;
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

    [HttpGet("product-statistics")]
    public async Task<IActionResult> GetProductStatistics([FromQuery] ProductStatisticsFilterDto filter)
    {
        try
        {
            var result = await _reportService.GetProductStatisticsAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("supplier-statistics")]
    public async Task<IActionResult> GetSupplierStatistics([FromQuery] SupplierStatisticsFilterDto filter)
    {
        try
        {
            var result = await _reportService.GetSupplierStatisticsAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("stocktake-statistics")]
    public async Task<IActionResult> GetStocktakeStatistics([FromQuery] StocktakeStatisticsFilterDto filter)
    {
        try
        {
            var result = await _reportService.GetStocktakeStatisticsAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockAlerts([FromQuery] LowStockAlertFilterDto filter)
    {
        try
        {
            var result = await _reportService.GetLowStockAlertsAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("expiring-lots")]
    public async Task<IActionResult> GetExpiringLotAlerts([FromQuery] ExpiringLotAlertFilterDto filter)
    {
        try
        {
            var result = await _reportService.GetExpiringLotAlertsAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("inventory/export")]
    public async Task<IActionResult> ExportInventory([FromQuery] InventoryReportFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetInventoryReportAsync(filter);
        var fileContent = _excelExportService.ExportInventory(result.Data.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InventoryReport.xlsx");
    }

    [HttpGet("inbound/export")]
    public async Task<IActionResult> ExportInbound([FromQuery] InboundReportFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetInboundReportAsync(filter);
        var fileContent = _excelExportService.ExportInbound(result.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InboundReport.xlsx");
    }

    [HttpGet("outbound/export")]
    public async Task<IActionResult> ExportOutbound([FromQuery] OutboundReportFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetOutboundReportAsync(filter);
        var fileContent = _excelExportService.ExportOutbound(result.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OutboundReport.xlsx");
    }

    [HttpGet("inoutstock/export")]
    public async Task<IActionResult> ExportInOutStock([FromQuery] InOutStockReportFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetInOutStockReportAsync(filter);
        var fileContent = _excelExportService.ExportInOutStock(result.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InOutStockReport.xlsx");
    }

    [HttpGet("product-statistics/export")]
    public async Task<IActionResult> ExportProductStatistics([FromQuery] ProductStatisticsFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetProductStatisticsAsync(filter);
        var fileContent = _excelExportService.ExportProductStatistics(result.Data.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ProductStatistics.xlsx");
    }

    [HttpGet("supplier-statistics/export")]
    public async Task<IActionResult> ExportSupplierStatistics([FromQuery] SupplierStatisticsFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetSupplierStatisticsAsync(filter);
        var fileContent = _excelExportService.ExportSupplierStatistics(result.Data.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SupplierStatistics.xlsx");
    }

    [HttpGet("stocktake-statistics/export")]
    public async Task<IActionResult> ExportStocktakeStatistics([FromQuery] StocktakeStatisticsFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetStocktakeStatisticsAsync(filter);
        var fileContent = _excelExportService.ExportStocktakeStatistics(result.Data.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StocktakeStatistics.xlsx");
    }

    [HttpGet("low-stock/export")]
    public async Task<IActionResult> ExportLowStockAlerts([FromQuery] LowStockAlertFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetLowStockAlertsAsync(filter);
        var fileContent = _excelExportService.ExportLowStockAlerts(result.Data.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LowStockAlerts.xlsx");
    }

    [HttpGet("expiring-lots/export")]
    public async Task<IActionResult> ExportExpiringLotAlerts([FromQuery] ExpiringLotAlertFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetExpiringLotAlertsAsync(filter);
        var fileContent = _excelExportService.ExportExpiringLotAlerts(result.Data.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ExpiringLotAlerts.xlsx");
    }
}
