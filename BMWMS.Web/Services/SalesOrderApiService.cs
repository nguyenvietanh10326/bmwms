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

    }
}

