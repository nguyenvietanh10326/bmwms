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

    public async Task<List<SourceOrderDropdownDto>> GetPendingPurchaseOrdersAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<SourceOrderDropdownDto>>("api/inbounds/purchase-orders/pending");
        return response ?? new List<SourceOrderDropdownDto>();
    }

    public async Task<List<AvailableWarehouseStaffDto>> GetAvailableWarehouseStaffAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<AvailableWarehouseStaffDto>>("api/inbounds/staff/available")
            ?? new List<AvailableWarehouseStaffDto>();
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

    public async Task ConfirmInboundOrderAsync(long id)
    {
        var response = await _httpClient.PutAsync($"api/inbounds/{id}/confirm", null);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Xác nhận thất bại: {error}");
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

    public async Task<long> ReceiveItemAsync(long id, ReceiveInboundItemDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/inbounds/{id}/receive", dto);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<long>();
        }
        var error = await response.Content.ReadAsStringAsync();
        throw new Exception(error);
    }

    public async Task ReceiveBatchAsync(long inboundOrderId, ReceiveBatchInboundDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/inbounds/{inboundOrderId}/receive-batch", dto);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Lỗi khi nhận hàng hàng loạt: {error}");
        }
    }

    public async Task PutawayBatchAsync(long id, List<PutawayInboundItemDto> dtos)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/inbounds/{id}/putaway", dtos);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Xếp vị trí thất bại: {error}");
        }
    }
}
