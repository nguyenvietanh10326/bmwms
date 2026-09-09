using BMWMS.Web.Models.Warehouse;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;

namespace BMWMS.Web.Pages.Warehouse
{
    public class EditModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public EditModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public UpdateWarehouseDto Warehouse { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public long Id { get; set; }

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet(long id) => RedirectToPage("/StorageLocations/Index");

        public IActionResult OnPost(long id) => RedirectToPage("/StorageLocations/Index");
    }
}
