using BMWMS.Web.Models.Inventory;
using BMWMS.Web.Models.Warehouse;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BMWMS.Web.Services
{
    public class SalesOrderApiService
    {
        private readonly HttpClient _httpClient;

        public SalesOrderApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<PagedResultDto<SalesOrderListDto>?> GetPagedOrdersAsync(SalesOrderFilterDto filter)
        {
            var queryString = $"?pageIndex={filter.PageIndex}&pageSize={filter.PageSize}";
            if (!string.IsNullOrEmpty(filter.SearchTerm)) queryString += $"&searchTerm={filter.SearchTerm}";
            if (!string.IsNullOrEmpty(filter.Status)) queryString += $"&status={filter.Status}";
            if (filter.WarehouseId.HasValue) queryString += $"&warehouseId={filter.WarehouseId.Value}";

            return await _httpClient.GetFromJsonAsync<PagedResultDto<SalesOrderListDto>>($"api/SalesOrders{queryString}");
        }

        public async Task<SalesOrderDetailDto?> GetOrderDetailAsync(long id)
        {
            return await _httpClient.GetFromJsonAsync<SalesOrderDetailDto>($"api/SalesOrders/{id}");
        }

        public async Task<List<UserLookupDto>> GetAllUsersAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<UserLookupDto>>("api/SalesOrders/AllUser") ?? new();
        }

        public async Task<List<WarehouseLookupDto>> GetAllWarehousesAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<WarehouseLookupDto>>("api/SalesOrders/AllWarehouse") ?? new();
        }

        public async Task<List<ProductLookupDto>> GetAllProductsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<ProductLookupDto>>("api/SalesOrders/AllProduct") ?? new();
        }

        public class UserLookupDto
        {
            public long UserId { get; set; }
            public string FullName { get; set; } = string.Empty;
        }

        public class WarehouseLookupDto
        {
            public long WarehouseId { get; set; }
            public string WarehouseName { get; set; } = string.Empty;
        }

        public class ProductLookupDto
        {
            public long ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string UnitName { get; set; } = string.Empty;
        }
    }
}

