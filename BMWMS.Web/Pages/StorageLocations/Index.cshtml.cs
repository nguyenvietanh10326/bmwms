using BMWMS.Web.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
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

        [BindProperty(SupportsGet = true)]
        public string ViewMode { get; set; } = "matrix";

        public StorageLocationPageDto Data { get; set; } = new();

        public WarehouseStructureDto StructureData { get; set; } = new();

        public List<WarehouseZoneDto> Zones { get; set; } = new();

        public List<StorageRackDto> Racks { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // 1. Fetch Paged Locations
            var query = $"?warehouseId={Filter.WarehouseId}&keyword={Filter.Keyword}&locationType={Filter.LocationType}&status={Filter.Status}&zoneId={Filter.ZoneId}&rackId={Filter.RackId}&pageIndex={Filter.PageIndex}&pageSize={Filter.PageSize}";
            var response = await client.GetAsync($"api/storagelocations{query}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Data = JsonSerializer.Deserialize<StorageLocationPageDto>(content, options) ?? new();
            }

            // 2. Fetch Structure Data for Hierarchy Matrix
            var structResponse = await client.GetAsync($"api/storagelocations/structure?warehouseId={Filter.WarehouseId}");
            if (structResponse.IsSuccessStatusCode)
            {
                var structContent = await structResponse.Content.ReadAsStringAsync();
                StructureData = JsonSerializer.Deserialize<WarehouseStructureDto>(structContent, options) ?? new();
            }

            // 3. Fetch Zones for Filters and Modals
            var zoneResponse = await client.GetAsync($"api/storagelocations/zones?warehouseId={Filter.WarehouseId}");
            if (zoneResponse.IsSuccessStatusCode)
            {
                var zoneContent = await zoneResponse.Content.ReadAsStringAsync();
                Zones = JsonSerializer.Deserialize<List<WarehouseZoneDto>>(zoneContent, options) ?? new();
            }

            // 4. Fetch Racks for Filters and Modals
            var rackResponse = await client.GetAsync($"api/storagelocations/racks?warehouseId={Filter.WarehouseId}");
            if (rackResponse.IsSuccessStatusCode)
            {
                var rackContent = await rackResponse.Content.ReadAsStringAsync();
                Racks = JsonSerializer.Deserialize<List<StorageRackDto>>(rackContent, options) ?? new();
            }

            return Page();
        }

        public async Task<IActionResult> OnGetExportAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var query = $"?warehouseId={Filter.WarehouseId}&keyword={Filter.Keyword}&locationType={Filter.LocationType}&status={Filter.Status}&zoneId={Filter.ZoneId}&rackId={Filter.RackId}&pageIndex=1&pageSize=10000";
            var response = await client.GetAsync($"api/storagelocations{query}");
            if (!response.IsSuccessStatusCode) return Page();

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var exportData = JsonSerializer.Deserialize<StorageLocationPageDto>(content, options);

            var sb = new StringBuilder();
            sb.AppendLine("Mã Vị Trí,Tên Vị Trí,Khu Vực (Zone),Dãy Kệ (Rack),Loại Vị Trí,Diện Tích (m2),Tải Trọng Max (kg),Thể Tích Max (m3),Trạng Thái,Số SP,Số Lô,Tổng Tồn");

            if (exportData?.Items != null)
            {
                foreach (var item in exportData.Items)
                {
                    sb.AppendLine($"\"{item.LocationCode}\",\"{item.LocationName}\",\"{item.ZoneCode} - {item.ZoneName}\",\"{item.RackCode} - {item.RackName}\",\"{item.LocationType}\",{item.AreaSquareMeter},{item.MaxWeightKg},{item.MaxVolumeM3},\"{item.Status}\",{item.StoredProductCount},{item.StoredLotCount},{item.TotalOnHandQuantity}");
                }
            }

            var preamble = Encoding.UTF8.GetPreamble();
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fullBytes = new byte[preamble.Length + bytes.Length];
            Buffer.BlockCopy(preamble, 0, fullBytes, 0, preamble.Length);
            Buffer.BlockCopy(bytes, 0, fullBytes, preamble.Length, bytes.Length);

            return File(fullBytes, "text/csv", $"StorageLocations_WH{Filter.WarehouseId}_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        public Task<IActionResult> OnGetLocationAsync(long id) =>
            ProxyGetAsync($"api/storagelocations/{id}");

        public Task<IActionResult> OnGetLocationInventoryAsync(long id) =>
            ProxyGetAsync($"api/storagelocations/{id}/inventory");

        public Task<IActionResult> OnGetSearchProductAsync(long warehouseId, string keyword = "") =>
            ProxyGetAsync(
                $"api/storagelocations/search-product?warehouseId={warehouseId}" +
                $"&keyword={Uri.EscapeDataString(keyword ?? string.Empty)}");

        public async Task<IActionResult> OnPostSaveLocationAsync(
            [FromBody] CreateUpdateStorageLocationDto dto)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = dto.StorageLocationId > 0
                ? await client.PutAsJsonAsync($"api/storagelocations/{dto.StorageLocationId}", dto)
                : await client.PostAsJsonAsync("api/storagelocations", dto);
            return await ProxyResponseAsync(response);
        }

        public async Task<IActionResult> OnPostCreateZoneAsync([FromBody] CreateUpdateZoneDto dto)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            return await ProxyResponseAsync(
                await client.PostAsJsonAsync("api/storagelocations/zones", dto));
        }

        public async Task<IActionResult> OnPostCreateRackAsync([FromBody] CreateUpdateRackDto dto)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            return await ProxyResponseAsync(
                await client.PostAsJsonAsync("api/storagelocations/racks", dto));
        }

        private async Task<IActionResult> ProxyGetAsync(string requestUri)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            return await ProxyResponseAsync(await client.GetAsync(requestUri));
        }

        private static async Task<IActionResult> ProxyResponseAsync(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json; charset=utf-8",
                Content = content
            };
        }
    }
}
