using System.Net.Http.Json;
using BMWMS.Web.Models;

namespace BMWMS.Web.Services;

public class WarehouseApiService
{
    private readonly HttpClient _httpClient;

    public WarehouseApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
    }

    public async Task<List<WarehouseModel>> GetWarehousesAsync()
    {
        var response = await _httpClient.GetAsync("/api/warehouses");
        if (!response.IsSuccessStatusCode) return new List<WarehouseModel>();
        
        var result = await response.Content.ReadFromJsonAsync<PagedResultModel<WarehouseModel>>();
        return result?.Items.ToList() ?? new List<WarehouseModel>();
    }
}
