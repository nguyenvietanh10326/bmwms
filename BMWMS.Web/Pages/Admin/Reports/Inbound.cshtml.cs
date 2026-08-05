using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.Admin.Reports
{
    public class InboundModel : PageModel
    {
        private readonly IReportApiService _reportApiService;

        public InboundModel(IReportApiService reportApiService)
        {
            _reportApiService = reportApiService;
        }

        [BindProperty(SupportsGet = true)]
        public InboundReportFilterModel Filter { get; set; } = new InboundReportFilterModel();

        public InboundReportResponseModel ReportData { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Default filter to current month if not set
            if (!Filter.FromDate.HasValue)
            {
                var now = DateTime.Today;
                Filter.FromDate = new DateTime(now.Year, now.Month, 1);
            }
            
            if (!Filter.ToDate.HasValue)
            {
                Filter.ToDate = DateTime.Today;
            }

            ReportData = await _reportApiService.GetInboundReportAsync(Filter);
            return Page();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            var fileBytes = await _reportApiService.ExportInboundAsync(Filter);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InboundReport.xlsx");
        }
    }
}
