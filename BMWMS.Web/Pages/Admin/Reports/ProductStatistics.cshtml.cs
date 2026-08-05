using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Reports;

public class ProductStatisticsModel : PageModel
{
    private readonly IReportApiService _reportApiService;

    public ProductStatisticsModel(IReportApiService reportApiService)
    {
        _reportApiService = reportApiService;
    }

    [BindProperty(SupportsGet = true)]
    public ProductStatisticsFilterModel Filter { get; set; } = new();

    public ProductStatisticsResponseModel ReportData { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        ReportData = await _reportApiService.GetProductStatisticsAsync(Filter);
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var fileBytes = await _reportApiService.ExportProductStatisticsAsync(Filter);
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ProductStatistics.xlsx");
    }
}
