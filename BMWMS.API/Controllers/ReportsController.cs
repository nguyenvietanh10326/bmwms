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

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,PURCHASING_STAFF,SALES_STAFF")]
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

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,PURCHASING_STAFF")]
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

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,SALES_STAFF")]
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

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,PURCHASING_STAFF,SALES_STAFF")]
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

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,SALES_STAFF")]
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

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF")]
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

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
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

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,PURCHASING_STAFF,SALES_STAFF")]
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

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
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

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,SALES_STAFF")]
    [HttpGet("inventory/export")]
    public async Task<IActionResult> ExportInventory([FromQuery] InventoryReportFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetInventoryReportAsync(filter);
        var fileContent = _excelExportService.ExportInventory(result.Data.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InventoryReport.xlsx");
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,SALES_STAFF")]
    [HttpGet("inbound/export")]
    public async Task<IActionResult> ExportInbound([FromQuery] InboundReportFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetInboundReportAsync(filter);
        var fileContent = _excelExportService.ExportInbound(result.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InboundReport.xlsx");
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,SALES_STAFF")]
    [HttpGet("outbound/export")]
    public async Task<IActionResult> ExportOutbound([FromQuery] OutboundReportFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetOutboundReportAsync(filter);
        var fileContent = _excelExportService.ExportOutbound(result.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OutboundReport.xlsx");
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,SALES_STAFF")]
    [HttpGet("inoutstock/export")]
    public async Task<IActionResult> ExportInOutStock([FromQuery] InOutStockReportFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetInOutStockReportAsync(filter);
        var fileContent = _excelExportService.ExportInOutStock(result.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InOutStockReport.xlsx");
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,SALES_STAFF")]
    [HttpGet("product-statistics/export")]
    public async Task<IActionResult> ExportProductStatistics([FromQuery] ProductStatisticsFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetProductStatisticsAsync(filter);
        var fileContent = _excelExportService.ExportProductStatistics(result.Data.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ProductStatistics.xlsx");
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,SALES_STAFF")]
    [HttpGet("supplier-statistics/export")]
    public async Task<IActionResult> ExportSupplierStatistics([FromQuery] SupplierStatisticsFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetSupplierStatisticsAsync(filter);
        var fileContent = _excelExportService.ExportSupplierStatistics(result.Data.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SupplierStatistics.xlsx");
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,SALES_STAFF")]
    [HttpGet("stocktake-statistics/export")]
    public async Task<IActionResult> ExportStocktakeStatistics([FromQuery] StocktakeStatisticsFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetStocktakeStatisticsAsync(filter);
        var fileContent = _excelExportService.ExportStocktakeStatistics(result.Data.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StocktakeStatistics.xlsx");
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,SALES_STAFF")]
    [HttpGet("low-stock/export")]
    public async Task<IActionResult> ExportLowStockAlerts([FromQuery] LowStockAlertFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetLowStockAlertsAsync(filter);
        var fileContent = _excelExportService.ExportLowStockAlerts(result.Data.Items);
        return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LowStockAlerts.xlsx");
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF,SALES_STAFF")]
    [HttpGet("expiring-lots/export")]
    public async Task<IActionResult> ExportExpiringLotAlerts([FromQuery] ExpiringLotAlertFilterDto filter)
    {
        filter.PageSize = 100000;
        var result = await _reportService.GetExpiringLotAlertsAsync(filter);
        var fileBytes = _excelExportService.ExportExpiringLotAlerts(result.Data.Items);
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ExpiringLotAlerts.xlsx");
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,PURCHASING_STAFF,SALES_STAFF")]
    [HttpGet("overdue-orders")]
    public async Task<IActionResult> GetOverdueOrderAlerts([FromQuery] OverdueOrderAlertFilterDto filter)
    {
        var result = await _reportService.GetOverdueOrderAlertsAsync(filter);
        return Ok(result);
    }

    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,PURCHASING_STAFF,SALES_STAFF")]
    [HttpGet("overdue-orders/export")]
    public async Task<IActionResult> ExportOverdueOrderAlerts([FromQuery] OverdueOrderAlertFilterDto filter)
    {
        filter.PageNumber = 1;
        filter.PageSize = 100000; // Export all data
        var result = await _reportService.GetOverdueOrderAlertsAsync(filter);
        var fileBytes = _excelExportService.ExportOverdueOrderAlerts(result.Data.Items);
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OverdueOrderAlerts.xlsx");
    }

    [HttpGet("warehouse-kpi")]
    public async Task<IActionResult> GetWarehouseKpis()
    {
        var result = await _reportService.GetWarehouseKpisAsync();
        return Ok(result);
    }
}
