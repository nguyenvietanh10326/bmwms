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

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Id = id;
            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync($"api/warehouses/{id}");

            if (!response.IsSuccessStatusCode)
            {
                return RedirectToPage("./Index");
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var detail = JsonSerializer.Deserialize<WarehouseResponseDto>(content, options);

            if (detail == null)
            {
                return RedirectToPage("./Index");
            }

            // Bind dữ liệu từ API sang UpdateWarehouseDto
            Warehouse = new UpdateWarehouseDto
            {
                WarehouseCode = detail.WarehouseCode,
                WarehouseName = detail.WarehouseName,
                Address = detail.Address,
                PhoneNumber = detail.PhoneNumber,
                IsPrimary = detail.IsPrimary,
                Status = string.IsNullOrEmpty(detail.Status) ? "ACTIVE" : detail.Status.ToUpper()
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long id)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var client = _httpClientFactory.CreateClient("ApiClient");

            // Đảm bảo Status gửi lên đúng định dạng regex UPPERCASE (ACTIVE / INACTIVE)
            Warehouse.Status = Warehouse.Status?.ToUpper() ?? "ACTIVE";

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(Warehouse),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PutAsync($"api/warehouses/{id}", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToPage("./Index");
            }

            var errorResponse = await response.Content.ReadAsStringAsync();
            ErrorMessage = string.IsNullOrWhiteSpace(errorResponse)
                ? "Failed to update warehouse. Please check your input."
                : errorResponse;

            return Page();
        }
    }
}
