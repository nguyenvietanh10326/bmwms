using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Reports;

public class InventoryModel : PageModel
{
    private readonly IReportApiService _reportApiService;

    public InventoryModel(IReportApiService reportApiService)
    {
        _reportApiService = reportApiService;
    }

    [BindProperty(SupportsGet = true)]
    public InventoryReportFilterModel Filter { get; set; } = new();

    public InventoryReportResponseModel ReportData { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var data = await _reportApiService.GetInventoryReportAsync(Filter);
        if (data != null)
        {
            ReportData = data;
        }

        return Page();
    }
}
