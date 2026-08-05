using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMWMS.Web.Pages.Admin.Reports;

public class OverdueOrderModel : PageModel
{
    private readonly IReportApiService _reportApiService;

    public OverdueOrderModel(IReportApiService reportApiService)
    {
        _reportApiService = reportApiService;
    }

    [BindProperty(SupportsGet = true)]
    public OverdueOrderAlertFilterModel Filter { get; set; } = new();

    public OverdueOrderAlertResponseModel ReportData { get; set; } = new();

    public List<SelectListItem> DocumentTypes { get; } = new()
    {
        new SelectListItem { Value = "", Text = "Tất cả" },
        new SelectListItem { Value = "INBOUND", Text = "Phiếu nhập kho" },
        new SelectListItem { Value = "OUTBOUND", Text = "Phiếu xuất kho" }
    };

    public async Task<IActionResult> OnGetAsync()
    {
        ReportData = await _reportApiService.GetOverdueOrderAlertsAsync(Filter);
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        var fileBytes = await _reportApiService.ExportOverdueOrderAlertsAsync(Filter);
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OverdueOrders.xlsx");
    }
}
