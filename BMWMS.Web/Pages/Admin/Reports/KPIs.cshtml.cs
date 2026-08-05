using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Reports;

public class KPIsModel : PageModel
{
    private readonly IReportApiService _reportApiService;

    public KPIsModel(IReportApiService reportApiService)
    {
        _reportApiService = reportApiService;
    }

    public WarehouseKpiResponseModel ReportData { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            ReportData = await _reportApiService.GetWarehouseKpisAsync();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "Không thể tải dữ liệu KPI. Chi tiết: " + ex.Message);
        }

        return Page();
    }
}
