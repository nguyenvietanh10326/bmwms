using BMWMS.Web.Models.Warehouse;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace BMWMS.Web.Pages.Warehouse
{
    public class DetailsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DetailsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public WarehouseResponseDto Warehouse { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            var response = await client.GetAsync($"api/warehouses/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return RedirectToPage("./Index");
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var result = JsonSerializer.Deserialize<WarehouseResponseDto>(content, options);

            if (result == null)
            {
                return RedirectToPage("./Index");
            }

            Warehouse = result;
            return Page();
        }
    }
}
