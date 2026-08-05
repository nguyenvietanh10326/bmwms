using BMWMS.Web.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;

namespace BMWMS.Web.Pages.StorageLocations
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public StorageLocationFilterDto Filter { get; set; } = new() { WarehouseId = 1, PageSize = 10 };

        public StorageLocationPageDto Data { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var query = $"?warehouseId={Filter.WarehouseId}&keyword={Filter.Keyword}&locationType={Filter.LocationType}&status={Filter.Status}&pageIndex={Filter.PageIndex}&pageSize={Filter.PageSize}";

            var response = await client.GetAsync($"api/storagelocations{query}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                Data = JsonSerializer.Deserialize<StorageLocationPageDto>(content, options) ?? new();
            }

            return Page();
        }

        // Handler cho Nút Export Excel/CSV
        public async Task<IActionResult> OnGetExportAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var query = $"?warehouseId={Filter.WarehouseId}&keyword={Filter.Keyword}&locationType={Filter.LocationType}&status={Filter.Status}&pageIndex=1&pageSize=10000";

            var response = await client.GetAsync($"api/storagelocations{query}");
            if (!response.IsSuccessStatusCode) return Page();

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<StorageLocationPageDto>(content, options);

            var builder = new StringBuilder();
            builder.AppendLine("Location Code,Location Name,Location Type,Zone,Floor,Capacity,Status");

            if (result?.Items != null)
            {
                foreach (var item in result.Items)
                {
                    builder.AppendLine($"\"{item.LocationCode}\",\"{item.LocationName}\",\"{item.LocationType}\",\"{item.ZoneCode}\",{item.Floor},\"{item.DisplayCapacity}\",\"{item.Status}\"");
                }
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
            return File(bytes, "text/csv", $"StorageLocations_WH{Filter.WarehouseId}_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}
