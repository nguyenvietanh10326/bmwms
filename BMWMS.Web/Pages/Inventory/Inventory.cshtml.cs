using Microsoft.AspNetCore.Authorization;
using BMWMS.Web.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using System.Text.Json;

namespace BMWMS.Web.Pages.Inventory
{
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
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

        public async Task<IActionResult> OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            var query = $"?keyword={Filter.Keyword}&warehouseId={Filter.WarehouseId}&storageLocationId={Filter.StorageLocationId}&status={Filter.Status}&pageIndex={Filter.PageIndex}&pageSize={Filter.PageSize}";

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
            var query = $"?keyword={Filter.Keyword}&warehouseId={Filter.WarehouseId}&storageLocationId={Filter.StorageLocationId}&status={Filter.Status}&pageIndex=1&pageSize=10000";

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
    }

}

