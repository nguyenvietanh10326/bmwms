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

        public void OnGet()
        {
            // Khởi tạo thông tin mặc định khi load trang Create
            Warehouse.Status = "Active";
            Warehouse.IsPrimary = false;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var client = _httpClientFactory.CreateClient("ApiClient");

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(Warehouse),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("api/warehouses", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToPage("./Index");
            }

            var errorResponse = await response.Content.ReadAsStringAsync();
            ErrorMessage = string.IsNullOrWhiteSpace(errorResponse)
                ? "Failed to create warehouse. Please check your input."
                : errorResponse;

            return Page();
        }
    }
}
