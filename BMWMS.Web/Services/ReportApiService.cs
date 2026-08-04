using BMWMS.Web.Models;

namespace BMWMS.Web.Services;

public class ReportApiService : IReportApiService
{
    private readonly HttpClient _httpClient;

    public ReportApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
    }

    public async Task<InventoryReportResponseModel?> GetInventoryReportAsync(InventoryReportFilterModel filter)
    {
        var query = new List<string>();
        if (!string.IsNullOrEmpty(filter.ProductSearch)) query.Add($"ProductSearch={Uri.EscapeDataString(filter.ProductSearch)}");
        if (!string.IsNullOrEmpty(filter.LocationCode)) query.Add($"LocationCode={Uri.EscapeDataString(filter.LocationCode)}");
        if (!string.IsNullOrEmpty(filter.LotNumber)) query.Add($"LotNumber={Uri.EscapeDataString(filter.LotNumber)}");
        query.Add($"PositiveStockOnly={filter.PositiveStockOnly}");
        query.Add($"PageIndex={filter.PageIndex}");
        query.Add($"PageSize={filter.PageSize}");

        var url = $"/api/reports/inventory?{string.Join("&", query)}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InventoryReportResponseModel>() ?? new InventoryReportResponseModel();
    }

    public async Task<InboundReportResponseModel> GetInboundReportAsync(InboundReportFilterModel filter)
    {
        var query = new List<string>();
        if (filter.FromDate.HasValue) query.Add($"fromDate={Uri.EscapeDataString(filter.FromDate.Value.ToString("O"))}");
        if (filter.ToDate.HasValue) query.Add($"toDate={Uri.EscapeDataString(filter.ToDate.Value.ToString("O"))}");
        if (!string.IsNullOrEmpty(filter.ProductSearch)) query.Add($"productSearch={Uri.EscapeDataString(filter.ProductSearch)}");
        if (!string.IsNullOrEmpty(filter.Status)) query.Add($"status={Uri.EscapeDataString(filter.Status)}");

        var queryString = string.Join("&", query);
        var url = "/api/reports/inbound" + (query.Any() ? $"?{queryString}" : "");

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<InboundReportResponseModel>() ?? new InboundReportResponseModel();
    }

    public async Task<OutboundReportResponseModel> GetOutboundReportAsync(OutboundReportFilterModel filter)
    {
        var query = new List<string>();
        if (filter.FromDate.HasValue) query.Add($"fromDate={Uri.EscapeDataString(filter.FromDate.Value.ToString("O"))}");
        if (filter.ToDate.HasValue) query.Add($"toDate={Uri.EscapeDataString(filter.ToDate.Value.ToString("O"))}");
        if (!string.IsNullOrEmpty(filter.ProductSearch)) query.Add($"productSearch={Uri.EscapeDataString(filter.ProductSearch)}");
        if (!string.IsNullOrEmpty(filter.Status)) query.Add($"status={Uri.EscapeDataString(filter.Status)}");

        var queryString = string.Join("&", query);
        var url = "/api/reports/outbound" + (query.Any() ? $"?{queryString}" : "");
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<OutboundReportResponseModel>() ?? new OutboundReportResponseModel();
    }

    public async Task<InOutStockReportResponseModel> GetInOutStockReportAsync(InOutStockReportFilterModel filter)
    {
        var query = new List<string>();
        if (filter.FromDate.HasValue) query.Add($"fromDate={Uri.EscapeDataString(filter.FromDate.Value.ToString("O"))}");
        if (filter.ToDate.HasValue) query.Add($"toDate={Uri.EscapeDataString(filter.ToDate.Value.ToString("O"))}");
        if (!string.IsNullOrEmpty(filter.ProductSearch)) query.Add($"productSearch={Uri.EscapeDataString(filter.ProductSearch)}");
        query.Add($"pageNumber={filter.PageNumber}");
        query.Add($"pageSize={filter.PageSize}");

        var queryString = string.Join("&", query);
        var url = "/api/reports/inoutstock" + (query.Any() ? $"?{queryString}" : "");
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<InOutStockReportResponseModel>() ?? new InOutStockReportResponseModel();
    }
}
