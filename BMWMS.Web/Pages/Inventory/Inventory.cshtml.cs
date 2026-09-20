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
        public List<ZoneModel> Zones { get; set; } = new();
        public List<RackModel> Racks { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Filter.PageIndex = Filter.PageIndex < 1 ? 1 : Filter.PageIndex;
            Filter.PageSize = Filter.PageSize < 1 ? 10 : Filter.PageSize;

            if (Filter.WarehouseId is null || Filter.WarehouseId <= 0)
            {
                Filter.WarehouseId = 1;
            }

            if (Filter.ZoneId is <= 0)
            {
                Filter.ZoneId = null;
            }

            if (Filter.RackId is <= 0)
            {
                Filter.RackId = null;
            }

            if (Filter.StorageLocationId is <= 0)
            {
                Filter.StorageLocationId = null;
            }

            var client = _httpClientFactory.CreateClient("ApiClient");
            Warehouses = await GetWarehousesAsync(client);
            Zones = await GetZonesAsync(client, Filter.WarehouseId ?? 1);
            Racks = await GetRacksAsync(client, Filter.WarehouseId ?? 1, Filter.ZoneId);

            if (Filter.ZoneId.HasValue && Filter.ZoneId > 0 && !Zones.Any(z => z.ZoneId == Filter.ZoneId.Value))
            {
                Filter.ZoneId = null;
                Racks = await GetRacksAsync(client, Filter.WarehouseId ?? 1, null);
            }

            if (Filter.RackId.HasValue && Filter.RackId > 0 && !Racks.Any(r => r.RackId == Filter.RackId.Value))
            {
                Filter.RackId = null;
            }

            var allLocations = await GetStorageLocationsAsync(client, Filter.WarehouseId ?? 1, Filter.ZoneId, Filter.RackId);
            StorageLocations = allLocations.Where(x => x.TotalOnHandQuantity > 0).ToList();

            if (Filter.StorageLocationId.HasValue && !StorageLocations.Any(x => x.StorageLocationId == Filter.StorageLocationId.Value))
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
            builder.AppendLine("Mã sản phẩm,Tên sản phẩm,Nhóm sản phẩm,Kho - Vị trí,On Hand,Reserved,Available,Ngày nhập - Hạn dùng,Trạng thái");

            if (result?.Items != null)
            {
                foreach (var item in result.Items)
                {
                    builder.AppendLine($"\"{item.ProductCode}\",\"{item.ProductName}\",\"{item.ProductGroupName}\",\"{item.WarehouseAndBin}\",{item.OnHandQuantity},{item.ReservedQuantity},{item.AvailableQuantity},\"{item.LotAndExpiryDisplay}\",\"{item.Status}\"");
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
            var zoneId = Filter.ZoneId ?? 0;
            var rackId = Filter.RackId ?? 0;
            var status = Uri.EscapeDataString(Filter.Status ?? string.Empty);

            var currentPageIndex = pageIndex ?? Filter.PageIndex;
            var currentPageSize = pageSize ?? Filter.PageSize;

            return $"?keyword={keyword}&warehouseId={warehouseId}&storageLocationId={storageLocationId}&zoneId={zoneId}&rackId={rackId}&status={status}&pageIndex={currentPageIndex}&pageSize={currentPageSize}";
        }

        private static async Task<List<WarehouseModel>> GetWarehousesAsync(HttpClient client)
        {
            var response = await client.GetAsync("/api/warehouses");
            if (!response.IsSuccessStatusCode)
                return new List<WarehouseModel>();

            var payload = await response.Content.ReadFromJsonAsync<WarehousePageResult>();
            return payload?.Items ?? new List<WarehouseModel>();
        }

        private static async Task<List<StorageLocationModel>> GetStorageLocationsAsync(HttpClient client, long warehouseId, long? zoneId, long? rackId)
        {
            

            var response = await client.GetAsync($"/api/storagelocations?warehouseId={warehouseId}&zoneId={zoneId}&rackId={rackId}&pageIndex=1&pageSize=500");
            if (!response.IsSuccessStatusCode)
                return new List<StorageLocationModel>();

            var payload = await response.Content.ReadFromJsonAsync<StorageLocationPageResult>();
            return payload?.Items ?? new List<StorageLocationModel>();
        }

                private static async Task<List<ZoneModel>> GetZonesAsync(HttpClient client, long warehouseId)
        {
            var response = await client.GetAsync($"/api/storagelocations/zones?warehouseId={warehouseId}");
            if (!response.IsSuccessStatusCode)
                return new List<ZoneModel>();
            
            return await response.Content.ReadFromJsonAsync<List<ZoneModel>>() ?? new List<ZoneModel>();
        }

        private static async Task<List<RackModel>> GetRacksAsync(HttpClient client, long warehouseId, long? zoneId)
        {
            var url = $"/api/storagelocations/racks?warehouseId={warehouseId}";
            if (zoneId.HasValue) url += $"&zoneId={zoneId.Value}";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return new List<RackModel>();
            
            return await response.Content.ReadFromJsonAsync<List<RackModel>>() ?? new List<RackModel>();
        }

        private sealed class WarehousePageResult
        {
            public List<WarehouseModel> Items { get; set; } = new();
        }

                public class ZoneModel { public long ZoneId { get; set; } public string ZoneCode { get; set; } = string.Empty; public string ZoneName { get; set; } = string.Empty; }
        public class RackModel { public long RackId { get; set; } public string RackCode { get; set; } = string.Empty; public string RackName { get; set; } = string.Empty; }
        private sealed class StorageLocationPageResult
        {
            public List<StorageLocationModel> Items { get; set; } = new();
        }

        public class ProductGroupOptionDto
        {
            public long ProductGroupId { get; set; }
            public string GroupCode { get; set; } = string.Empty;
            public string GroupName { get; set; } = string.Empty;
        }
    }

}



