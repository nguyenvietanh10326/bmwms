using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Reports;

public class StocktakeStatisticsModel : PageModel
{
    private readonly IReportApiService _reportApiService;

    public StocktakeStatisticsModel(IReportApiService reportApiService)
    {
        _reportApiService = reportApiService;
    }

    [BindProperty(SupportsGet = true)]
    public StocktakeStatisticsFilterModel Filter { get; set; } = new();

    public StocktakeStatisticsResponseModel ReportData { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        ReportData = await _reportApiService.GetStocktakeStatisticsAsync(Filter);
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var fileBytes = await _reportApiService.ExportStocktakeStatisticsAsync(Filter);
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "StocktakeStatistics.xlsx");
    }
}
