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
        return await _httpClient.GetFromJsonAsync<InventoryReportResponseModel>(url);
    }
}
