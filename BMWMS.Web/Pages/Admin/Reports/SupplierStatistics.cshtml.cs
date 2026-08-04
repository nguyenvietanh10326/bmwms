using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Reports;

public class SupplierStatisticsModel : PageModel
{
    private readonly IReportApiService _reportApiService;

    public SupplierStatisticsModel(IReportApiService reportApiService)
    {
        _reportApiService = reportApiService;
    }

    [BindProperty(SupportsGet = true)]
    public SupplierStatisticsFilterModel Filter { get; set; } = new();

    public SupplierStatisticsResponseModel ReportData { get; set; } = new();

    public async Task OnGetAsync()
    {
        ReportData = await _reportApiService.GetSupplierStatisticsAsync(Filter);
    }
}
