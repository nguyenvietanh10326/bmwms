using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BMWMS.Web.Models;
using Microsoft.AspNetCore.Mvc;

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
        if (!string.IsNullOrEmpty(filter.Keyword)) queryString += $"&keyword={Uri.EscapeDataString(filter.Keyword)}";
        if (!string.IsNullOrEmpty(filter.Status)) queryString += $"&status={Uri.EscapeDataString(filter.Status)}";
        if (!string.IsNullOrEmpty(filter.SourceType)) queryString += $"&sourceType={Uri.EscapeDataString(filter.SourceType)}";
        if (filter.FromDate.HasValue) queryString += $"&fromDate={filter.FromDate.Value:yyyy-MM-dd}";
        if (filter.ToDate.HasValue) queryString += $"&toDate={filter.ToDate.Value:yyyy-MM-dd}";
        if (filter.AssignedToUserId.HasValue) queryString += $"&assignedToUserId={filter.AssignedToUserId.Value}";

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
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await ReadErrorAsync(response));
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

    public async Task<List<PurchaseOrderInboundSourceDto>> GetPurchaseOrderInboundSourcesAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<PurchaseOrderInboundSourceDto>>(
                   "api/inbounds/purchase-orders/available")
               ?? new List<PurchaseOrderInboundSourceDto>();
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
            throw new Exception($"Cập nhật thất bại: {await ReadErrorAsync(response)}");
        }
    }

    public async Task ConfirmInboundOrderAsync(long id)
    {
        var response = await _httpClient.PutAsync($"api/inbounds/{id}/confirm", null);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Xác nhận thất bại: {await ReadErrorAsync(response)}");
        }
    }

    public async Task CancelInboundOrderAsync(long id, string reason)
    {
        var dto = new CancelInboundOrderDto { CancellationReason = reason };
        var response = await _httpClient.PutAsJsonAsync($"api/inbounds/{id}/cancel", dto);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Hủy thất bại: {await ReadErrorAsync(response)}");
        }
    }

    public async Task<long> ReceiveItemAsync(long id, ReceiveInboundItemDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/inbounds/{id}/receive", dto);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ReceiveItemResponse>();
            return result?.ProductLotId ?? throw new Exception("API không trả về mã lô vừa nhận.");
        }
        throw new Exception(await ReadErrorAsync(response));
    }

    public async Task CompleteReceiptAsync(long id, List<InboundReceiptDecisionDto> decisions, string? notes = null)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/inbounds/{id}/complete-receipt", new CompleteInboundReceiptDto
        {
            Notes = notes,
            Decisions = decisions
        });
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Hoàn tất kiểm nhận thất bại: {await ReadErrorAsync(response)}");
        }
    }

    public async Task ReceiveBatchAsync(long inboundOrderId, ReceiveBatchInboundDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/inbounds/{inboundOrderId}/receive-batch", dto);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Lưu kiểm nhận thất bại: {await ReadErrorAsync(response)}");
        }
    }

    public async Task PutawayBatchAsync(long id, PutawayBatchRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync($"/api/inbounds/{id}/putaway-with-capacity", request);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Xếp vị trí thất bại: {await ReadErrorAsync(response)}");
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            if (!string.IsNullOrWhiteSpace(problem?.Detail)) return problem.Detail;
            if (!string.IsNullOrWhiteSpace(problem?.Title)) return problem.Title;
        }
        catch
        {
            // API cũ có thể vẫn trả chuỗi thuần; đọc lại ở nhánh bên dưới nếu còn nội dung.
        }

        var raw = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(raw) ? $"API trả về mã {response.StatusCode}." : raw.Trim('"');
    }
}

public class ReceiveItemResponse
{
    public long ProductLotId { get; set; }
}
