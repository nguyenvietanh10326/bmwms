using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.Admin.Reports
{
    public class InOutStockModel : PageModel
    {
        private readonly IReportApiService _reportApiService;

        public InOutStockModel(IReportApiService reportApiService)
        {
            _reportApiService = reportApiService;
        }

        [BindProperty(SupportsGet = true)]
        public InOutStockReportFilterModel Filter { get; set; } = new();

        public InOutStockReportResponseModel ReportData { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            ReportData = await _reportApiService.GetInOutStockReportAsync(Filter);
            return Page();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            var fileBytes = await _reportApiService.ExportInOutStockAsync(Filter);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InOutStockReport.xlsx");
        }
    }
}
