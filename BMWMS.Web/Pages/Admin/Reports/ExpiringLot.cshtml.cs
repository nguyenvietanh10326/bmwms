using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Reports;

public class ExpiringLotModel : PageModel
{
    private readonly IReportApiService _reportApiService;

    public ExpiringLotModel(IReportApiService reportApiService)
    {
        _reportApiService = reportApiService;
    }

    [BindProperty(SupportsGet = true)]
    public ExpiringLotAlertFilterModel Filter { get; set; } = new();

    public ExpiringLotAlertResponseModel ReportData { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        ReportData = await _reportApiService.GetExpiringLotAlertsAsync(Filter);
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var fileBytes = await _reportApiService.ExportExpiringLotAlertsAsync(Filter);
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ExpiringLots.xlsx");
    }
}
