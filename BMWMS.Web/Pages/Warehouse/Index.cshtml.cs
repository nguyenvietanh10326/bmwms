using BMWMS.Web.Models.Warehouse;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace BMWMS.Web.Pages.Warehouse
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public WarehouseFilterDto Filter { get; set; } = new WarehouseFilterDto();

        public PagedResultDto<WarehouseResponseDto> WarehousesResult { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        public string? ErrorMessage { get; set; }

        public IActionResult OnGet() => RedirectToPage("/StorageLocations/Index");
    }
}
