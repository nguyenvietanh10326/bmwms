using BMWMS.Web.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace BMWMS.Web.Pages.Inventory
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public DashboardSummaryDto Summary { get; set; } = new();

        public IActionResult OnGet()
        {
            return RedirectToPage("/Inventory/Inventory");
        }
    }
}
