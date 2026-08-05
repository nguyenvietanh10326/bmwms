using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.Admin.Reports
{
    public class OutboundModel : PageModel
    {
        private readonly IReportApiService _reportApiService;

        public OutboundModel(IReportApiService reportApiService)
        {
            _reportApiService = reportApiService;
        }

        [BindProperty(SupportsGet = true)]
        public OutboundReportFilterModel Filter { get; set; } = new();

        public OutboundReportResponseModel ReportData { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            ReportData = await _reportApiService.GetOutboundReportAsync(Filter);
            return Page();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            var fileBytes = await _reportApiService.ExportOutboundAsync(Filter);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "OutboundReport.xlsx");
        }
    }
}
