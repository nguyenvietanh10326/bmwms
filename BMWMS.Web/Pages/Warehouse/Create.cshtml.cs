using BMWMS.Web.Models.Warehouse;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;

namespace BMWMS.Web.Pages.Warehouse
{
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CreateModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public CreateWarehouseDto Warehouse { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet() => RedirectToPage("/StorageLocations/Index");

        public IActionResult OnPost() => RedirectToPage("/StorageLocations/Index");
    }
}
