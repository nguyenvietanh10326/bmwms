using BMWMS.Web.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using BMWMS.Web.Services;
using BMWMS.Web.Helpers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Json;
using System.Text.Json;

namespace BMWMS.Web.Pages.StorageLocations
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ProductGroupApiService _productGroups;
        private readonly ProductApiService _products;

        public IndexModel(IHttpClientFactory httpClientFactory,
                          ProductGroupApiService productGroups, ProductApiService products)
        {
            _httpClientFactory = httpClientFactory;
            _productGroups = productGroups;
            _products = products;
        }

        public sealed class CapacityGroupOption
        {
            public long ProductGroupId { get; set; }
            public string GroupName { get; set; } = string.Empty;
            public string UnitName { get; set; } = string.Empty;
            public byte QuantityScale { get; set; }
        }

        public List<CapacityGroupOption> CapacityGroups { get; set; } = new();

        public string FormatCapacity(decimal? quantity, long? groupId) =>
            FormatHelper.FormatOptionalNumber(quantity,
                CapacityGroups.FirstOrDefault(group => group.ProductGroupId == groupId)?.QuantityScale ?? 4);

        [BindProperty(SupportsGet = true)]
        public StorageLocationFilterDto Filter { get; set; } = new() { WarehouseId = 1 };

        public WarehouseStructureDto StructureData { get; set; } = new();

        public List<WarehouseZoneDto> Zones { get; set; } = new();

        public List<StorageRackDto> Racks { get; set; } = new();

        public string? LoadError { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // BMWMS is a single-warehouse system. Do not allow a query string to
            // turn this screen back into an implicit multi-warehouse selector.
            Filter.WarehouseId = 1;

            var client = _httpClientFactory.CreateClient("ApiClient");
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var groupRows = await _productGroups.GetAllActiveAsync();
            var unitRows = (await _products.GetUnitsOfMeasureAsync())
                .ToDictionary(unit => unit.UnitOfMeasureId);
            CapacityGroups = groupRows
                .Where(group => group.BaseUnitOfMeasureId is int unitId && unitRows.ContainsKey(unitId))
                .Select(group =>
                {
                    var unit = unitRows[group.BaseUnitOfMeasureId!.Value];
                    return new CapacityGroupOption
                    {
                        ProductGroupId = group.ProductGroupId,
                        GroupName = group.GroupName,
                        UnitName = unit.UnitName,
                        QuantityScale = unit.QuantityScale
                    };
                }).ToList();

            using var structResponse = await client.GetAsync(
                $"api/storagelocations/structure?warehouseId={Filter.WarehouseId}");
            if (structResponse.IsSuccessStatusCode)
            {
                var structContent = await structResponse.Content.ReadAsStringAsync();
                StructureData = JsonSerializer.Deserialize<WarehouseStructureDto>(structContent, options) ?? new();
                BuildEditCatalogs();
            }
            else
            {
                LoadError = "Không tải được dữ liệu sơ đồ vị trí. Vui lòng tải lại trang hoặc kiểm tra kết nối API.";
            }

            return Page();
        }

        private void BuildEditCatalogs()
        {
            Zones = StructureData.Zones.Select(zone => new WarehouseZoneDto
            {
                ZoneId = zone.ZoneId,
                WarehouseId = Filter.WarehouseId,
                ZoneCode = zone.ZoneCode,
                ZoneName = zone.ZoneName,
                Description = zone.Description,
                ProductGroupId = zone.ProductGroupId,
                MaxCapacityQuantity = zone.MaxCapacityQuantity,
                MaxWeightKg = zone.MaxWeightKg,
                MaxVolumeM3 = zone.MaxVolumeM3,
                Status = zone.Status
            }).ToList();

            Racks = StructureData.Zones.SelectMany(zone => zone.Racks.Select(rack => new StorageRackDto
            {
                RackId = rack.RackId,
                WarehouseId = Filter.WarehouseId,
                ZoneId = zone.ZoneId,
                ZoneCode = zone.ZoneCode,
                ZoneName = zone.ZoneName,
                RackCode = rack.RackCode,
                RackName = rack.RackName,
                MaxCapacityQuantity = rack.MaxCapacityQuantity,
                MaxWeightKg = rack.MaxWeightKg,
                MaxVolumeM3 = rack.MaxVolumeM3,
                Status = rack.Status
            })).ToList();
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

        public async Task<IActionResult> OnPostSaveZoneAsync([FromBody] CreateUpdateZoneDto dto)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = dto.ZoneId > 0
                ? await client.PutAsJsonAsync($"api/storagelocations/zones/{dto.ZoneId}", dto)
                : await client.PostAsJsonAsync("api/storagelocations/zones", dto);
            return await ProxyResponseAsync(response);
        }

        public async Task<IActionResult> OnPostSaveRackAsync([FromBody] CreateUpdateRackDto dto)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = dto.RackId > 0
                ? await client.PutAsJsonAsync($"api/storagelocations/racks/{dto.RackId}", dto)
                : await client.PostAsJsonAsync("api/storagelocations/racks", dto);
            return await ProxyResponseAsync(response);
        }

        public Task<IActionResult> OnPostChangeStatusAsync(string nodeType, long id, [FromBody] ChangeStorageNodeStatusRequest request) =>
            ProxyPatchAsync($"api/storagelocations/{GetStatusPath(nodeType, id)}", request);

        private static string GetStatusPath(string nodeType, long id) => nodeType.ToLowerInvariant() switch
        {
            "zone" => $"zones/{id}/status",
            "rack" => $"racks/{id}/status",
            _ => $"{id}/status"
        };

        private async Task<IActionResult> ProxyPatchAsync(string requestUri, object body)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            return await ProxyResponseAsync(await client.PatchAsJsonAsync(requestUri, body));
        }

        public sealed class ChangeStorageNodeStatusRequest
        {
            public bool Active { get; set; }
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
