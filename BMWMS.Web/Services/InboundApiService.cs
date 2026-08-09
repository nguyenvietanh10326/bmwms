using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BMWMS.Web.Models;

namespace BMWMS.Web.Services;

public class InboundApiService
{
    private readonly HttpClient _httpClient;

    public InboundApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
    }

    public async Task<InboundOrderPageModel> GetInboundOrdersPageAsync(InboundOrderFilterModel filter)
    {
        var queryString = $"?pageIndex={filter.PageIndex}&pageSize={filter.PageSize}";
        if (!string.IsNullOrEmpty(filter.Keyword)) queryString += $"&keyword={filter.Keyword}";
        if (!string.IsNullOrEmpty(filter.Status)) queryString += $"&status={filter.Status}";
        if (filter.FromDate.HasValue) queryString += $"&fromDate={filter.FromDate.Value:yyyy-MM-dd}";
        if (filter.ToDate.HasValue) queryString += $"&toDate={filter.ToDate.Value:yyyy-MM-dd}";

        var response = await _httpClient.GetFromJsonAsync<InboundOrderPageModel>($"api/inbounds{queryString}");
        return response ?? new InboundOrderPageModel();
    }

    public async Task<InboundOrderDetailDto?> GetInboundOrderByIdAsync(long id)
    {
        return await _httpClient.GetFromJsonAsync<InboundOrderDetailDto>($"api/inbounds/{id}");
    }

    public async Task<long> CreateInboundOrderAsync(CreateInboundOrderDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/inbounds", dto);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<long>();
    }

    public async Task<PurchaseOrderForInboundDto?> GetPurchaseOrderForInboundAsync(long poId)
    {
        var response = await _httpClient.GetAsync($"api/inbounds/purchase-orders/{poId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<PurchaseOrderForInboundDto>();
        }
        return null;
    }

    public async Task<List<ShortageInboundOrderDto>> GetShortageInboundOrdersAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<ShortageInboundOrderDto>>("api/inbounds/shortages");
        return response ?? new List<ShortageInboundOrderDto>();
    }

    public async Task<PurchaseOrderForInboundDto?> GetInboundOrderForSupplementAsync(long parentId)
    {
        var response = await _httpClient.GetAsync($"api/inbounds/{parentId}/supplement");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<PurchaseOrderForInboundDto>();
        }
        return null;
    }

    public async Task<List<SourceOrderDropdownDto>> GetPendingPurchaseOrdersAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<SourceOrderDropdownDto>>("api/inbounds/purchase-orders/pending");
        return response ?? new List<SourceOrderDropdownDto>();
    }

    public async Task<List<SourceOrderDropdownDto>> GetReturnableSalesOrdersAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<SourceOrderDropdownDto>>("api/inbounds/sales-orders/returnable");
        return response ?? new List<SourceOrderDropdownDto>();
    }

    public async Task<PurchaseOrderForInboundDto?> GetSalesOrderForInboundAsync(long soId)
    {
        var response = await _httpClient.GetAsync($"api/inbounds/sales-orders/{soId}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<PurchaseOrderForInboundDto>();
        }
        return null;
    }

    public async Task UpdateInboundOrderAsync(long id, UpdateInboundOrderDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/inbounds/{id}", dto);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Cập nhật thất bại: {error}");
        }
    }

    public async Task CancelInboundOrderAsync(long id, string reason)
    {
        var dto = new CancelInboundOrderDto { CancellationReason = reason };
        var response = await _httpClient.PutAsJsonAsync($"api/inbounds/{id}/cancel", dto);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Hủy thất bại: {error}");
        }
    }
}
