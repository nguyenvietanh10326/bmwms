using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Reports;

public class LowStockModel : PageModel
{
    private readonly IReportApiService _reportApiService;

    public LowStockModel(IReportApiService reportApiService)
    {
        _reportApiService = reportApiService;
    }

    [BindProperty(SupportsGet = true)]
    public LowStockAlertFilterModel Filter { get; set; } = new();

    public LowStockAlertResponseModel ReportData { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        ReportData = await _reportApiService.GetLowStockAlertsAsync(Filter);
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var fileBytes = await _reportApiService.ExportLowStockAlertsAsync(Filter);
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LowStockAlerts.xlsx");
    }
}
