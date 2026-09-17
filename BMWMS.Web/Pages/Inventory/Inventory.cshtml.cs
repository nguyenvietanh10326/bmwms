using BMWMS.Web.Models;
using BMWMS.Web.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;

namespace BMWMS.Web.Pages.Inventory
{
    public class InventoryModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public InventoryModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public InventoryFilterDto Filter { get; set; } = new();

        public InventoryDashboardPageDto Data { get; set; } = new();
        public List<WarehouseModel> Warehouses { get; set; } = new();
        public List<StorageLocationModel> StorageLocations { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Filter.PageIndex = Filter.PageIndex < 1 ? 1 : Filter.PageIndex;
            Filter.PageSize = Filter.PageSize < 1 ? 10 : Filter.PageSize;

            var client = _httpClientFactory.CreateClient("ApiClient");
            Warehouses = await GetWarehousesAsync(client);
            StorageLocations = await GetStorageLocationsAsync(client, Filter.WarehouseId);

            if (Filter.WarehouseId is null || Filter.WarehouseId <= 0)
            {
                Filter.StorageLocationId = null;
            }
            else if (Filter.StorageLocationId.HasValue && !StorageLocations.Any(x => x.StorageLocationId == Filter.StorageLocationId.Value))
            {
                Filter.StorageLocationId = null;
            }

            var query = BuildInventoryQuery();
            var response = await client.GetAsync($"api/inventories{query}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                Data = JsonSerializer.Deserialize<InventoryDashboardPageDto>(content, options) ?? new();
            }

            return Page();
        }

        public async Task<IActionResult> OnGetExportExcelAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var query = BuildInventoryQuery(pageIndex: 1, pageSize: 10000);

            var response = await client.GetAsync($"api/inventories{query}");

            if (!response.IsSuccessStatusCode) return Page();

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<InventoryDashboardPageDto>(content, options);

            var builder = new StringBuilder();
            builder.AppendLine("Mã sản phẩm,Tên sản phẩm,Kho - Vị trí,On Hand,Reserved,Available,Lô - Hạn dùng,Trạng thái");

            if (result?.Items != null)
            {
                foreach (var item in result.Items)
                {
                    builder.AppendLine($"\"{item.ProductCode}\",\"{item.ProductName}\",\"{item.WarehouseAndBin}\",{item.OnHandQuantity},{item.ReservedQuantity},{item.AvailableQuantity},\"{item.LotAndExpiryDisplay}\",\"{item.Status}\"");
                }
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
            return File(bytes, "text/csv", $"TonKho_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        private string BuildInventoryQuery(int? pageIndex = null, int? pageSize = null)
        {
            var keyword = Uri.EscapeDataString(Filter.Keyword ?? string.Empty);
            var warehouseId = Filter.WarehouseId ?? 0;
            var storageLocationId = Filter.StorageLocationId ?? 0;
            var status = Uri.EscapeDataString(Filter.Status ?? string.Empty);

            var currentPageIndex = pageIndex ?? Filter.PageIndex;
            var currentPageSize = pageSize ?? Filter.PageSize;

            return $"?keyword={keyword}&warehouseId={warehouseId}&storageLocationId={storageLocationId}&status={status}&pageIndex={currentPageIndex}&pageSize={currentPageSize}";
        }

        private static async Task<List<WarehouseModel>> GetWarehousesAsync(HttpClient client)
        {
            var response = await client.GetAsync("/api/warehouses");
            if (!response.IsSuccessStatusCode)
                return new List<WarehouseModel>();

            var payload = await response.Content.ReadFromJsonAsync<WarehousePageResult>();
            return payload?.Items ?? new List<WarehouseModel>();
        }

        private static async Task<List<StorageLocationModel>> GetStorageLocationsAsync(HttpClient client, long? warehouseId)
        {
            if (!warehouseId.HasValue || warehouseId.Value <= 0)
                return new List<StorageLocationModel>();

            var response = await client.GetAsync($"/api/storagelocations?warehouseId={warehouseId}&pageIndex=1&pageSize=500");
            if (!response.IsSuccessStatusCode)
                return new List<StorageLocationModel>();

            var payload = await response.Content.ReadFromJsonAsync<StorageLocationPageResult>();
            return payload?.Items ?? new List<StorageLocationModel>();
        }

        private sealed class WarehousePageResult
        {
            public List<WarehouseModel> Items { get; set; } = new();
        }

        private sealed class StorageLocationPageResult
        {
            public List<StorageLocationModel> Items { get; set; } = new();
        }
    }

}
