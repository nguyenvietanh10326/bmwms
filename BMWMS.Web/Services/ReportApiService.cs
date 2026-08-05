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

    public async Task<ProductStatisticsResponseModel> GetProductStatisticsAsync(ProductStatisticsFilterModel filter)
    {
        var query = new List<string>();
        if (filter.FromDate.HasValue) query.Add($"fromDate={Uri.EscapeDataString(filter.FromDate.Value.ToString("O"))}");
        if (filter.ToDate.HasValue) query.Add($"toDate={Uri.EscapeDataString(filter.ToDate.Value.ToString("O"))}");
        if (!string.IsNullOrEmpty(filter.ProductSearch)) query.Add($"productSearch={Uri.EscapeDataString(filter.ProductSearch)}");
        if (!string.IsNullOrEmpty(filter.ProductGroupCode)) query.Add($"productGroupCode={Uri.EscapeDataString(filter.ProductGroupCode)}");
        query.Add($"pageNumber={filter.PageNumber}");
        query.Add($"pageSize={filter.PageSize}");

        var queryString = string.Join("&", query);
        var url = "/api/reports/product-statistics" + (query.Any() ? $"?{queryString}" : "");
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ProductStatisticsResponseModel>() ?? new ProductStatisticsResponseModel();
    }

    public async Task<SupplierStatisticsResponseModel> GetSupplierStatisticsAsync(SupplierStatisticsFilterModel filter)
    {
        var query = new List<string>();
        if (filter.FromDate.HasValue) query.Add($"fromDate={Uri.EscapeDataString(filter.FromDate.Value.ToString("O"))}");
        if (filter.ToDate.HasValue) query.Add($"toDate={Uri.EscapeDataString(filter.ToDate.Value.ToString("O"))}");
        if (!string.IsNullOrEmpty(filter.SupplierSearch)) query.Add($"supplierSearch={Uri.EscapeDataString(filter.SupplierSearch)}");
        query.Add($"pageNumber={filter.PageNumber}");
        query.Add($"pageSize={filter.PageSize}");

        var queryString = string.Join("&", query);
        var url = "/api/reports/supplier-statistics" + (query.Any() ? $"?{queryString}" : "");
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SupplierStatisticsResponseModel>() ?? new SupplierStatisticsResponseModel();
    }

    public async Task<StocktakeStatisticsResponseModel> GetStocktakeStatisticsAsync(StocktakeStatisticsFilterModel filter)
    {
        var query = new List<string>();
        if (filter.FromDate.HasValue) query.Add($"fromDate={Uri.EscapeDataString(filter.FromDate.Value.ToString("O"))}");
        if (filter.ToDate.HasValue) query.Add($"toDate={Uri.EscapeDataString(filter.ToDate.Value.ToString("O"))}");
        if (!string.IsNullOrEmpty(filter.SessionStatus)) query.Add($"sessionStatus={Uri.EscapeDataString(filter.SessionStatus)}");
        query.Add($"pageNumber={filter.PageNumber}");
        query.Add($"pageSize={filter.PageSize}");

        var queryString = string.Join("&", query);
        var url = "/api/reports/stocktake-statistics" + (query.Any() ? $"?{queryString}" : "");
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<StocktakeStatisticsResponseModel>() ?? new StocktakeStatisticsResponseModel();
    }

    public async Task<byte[]> ExportInventoryAsync(InventoryReportFilterModel filter)
    {
        var query = new List<string>();
        if (!string.IsNullOrEmpty(filter.ProductSearch)) query.Add($"ProductSearch={Uri.EscapeDataString(filter.ProductSearch)}");
        if (!string.IsNullOrEmpty(filter.LocationCode)) query.Add($"LocationCode={Uri.EscapeDataString(filter.LocationCode)}");
        if (!string.IsNullOrEmpty(filter.LotNumber)) query.Add($"LotNumber={Uri.EscapeDataString(filter.LotNumber)}");
        query.Add($"PositiveStockOnly={filter.PositiveStockOnly}");

        var url = $"/api/reports/inventory/export?{string.Join("&", query)}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<byte[]> ExportInboundAsync(InboundReportFilterModel filter)
    {
        var query = new List<string>();
        if (filter.FromDate.HasValue) query.Add($"fromDate={Uri.EscapeDataString(filter.FromDate.Value.ToString("O"))}");
        if (filter.ToDate.HasValue) query.Add($"toDate={Uri.EscapeDataString(filter.ToDate.Value.ToString("O"))}");
        if (!string.IsNullOrEmpty(filter.ProductSearch)) query.Add($"productSearch={Uri.EscapeDataString(filter.ProductSearch)}");
        if (!string.IsNullOrEmpty(filter.Status)) query.Add($"status={Uri.EscapeDataString(filter.Status)}");

        var queryString = string.Join("&", query);
        var url = "/api/reports/inbound/export" + (query.Any() ? $"?{queryString}" : "");

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<byte[]> ExportOutboundAsync(OutboundReportFilterModel filter)
    {
        var query = new List<string>();
        if (filter.FromDate.HasValue) query.Add($"fromDate={Uri.EscapeDataString(filter.FromDate.Value.ToString("O"))}");
        if (filter.ToDate.HasValue) query.Add($"toDate={Uri.EscapeDataString(filter.ToDate.Value.ToString("O"))}");
        if (!string.IsNullOrEmpty(filter.ProductSearch)) query.Add($"productSearch={Uri.EscapeDataString(filter.ProductSearch)}");
        if (!string.IsNullOrEmpty(filter.Status)) query.Add($"status={Uri.EscapeDataString(filter.Status)}");

        var queryString = string.Join("&", query);
        var url = "/api/reports/outbound/export" + (query.Any() ? $"?{queryString}" : "");
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<byte[]> ExportInOutStockAsync(InOutStockReportFilterModel filter)
    {
        var query = new List<string>();
        if (filter.FromDate.HasValue) query.Add($"fromDate={Uri.EscapeDataString(filter.FromDate.Value.ToString("O"))}");
        if (filter.ToDate.HasValue) query.Add($"toDate={Uri.EscapeDataString(filter.ToDate.Value.ToString("O"))}");
        if (!string.IsNullOrEmpty(filter.ProductSearch)) query.Add($"productSearch={Uri.EscapeDataString(filter.ProductSearch)}");

        var queryString = string.Join("&", query);
        var url = "/api/reports/inoutstock/export" + (query.Any() ? $"?{queryString}" : "");
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<byte[]> ExportProductStatisticsAsync(ProductStatisticsFilterModel filter)
    {
        var query = new List<string>();
        if (filter.FromDate.HasValue) query.Add($"fromDate={Uri.EscapeDataString(filter.FromDate.Value.ToString("O"))}");
        if (filter.ToDate.HasValue) query.Add($"toDate={Uri.EscapeDataString(filter.ToDate.Value.ToString("O"))}");
        if (!string.IsNullOrEmpty(filter.ProductSearch)) query.Add($"productSearch={Uri.EscapeDataString(filter.ProductSearch)}");
        if (!string.IsNullOrEmpty(filter.ProductGroupCode)) query.Add($"productGroupCode={Uri.EscapeDataString(filter.ProductGroupCode)}");

        var queryString = string.Join("&", query);
        var url = "/api/reports/product-statistics/export" + (query.Any() ? $"?{queryString}" : "");
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<byte[]> ExportSupplierStatisticsAsync(SupplierStatisticsFilterModel filter)
    {
        var query = new List<string>();
        if (filter.FromDate.HasValue) query.Add($"fromDate={Uri.EscapeDataString(filter.FromDate.Value.ToString("O"))}");
        if (filter.ToDate.HasValue) query.Add($"toDate={Uri.EscapeDataString(filter.ToDate.Value.ToString("O"))}");
        if (!string.IsNullOrEmpty(filter.SupplierSearch)) query.Add($"supplierSearch={Uri.EscapeDataString(filter.SupplierSearch)}");

        var queryString = string.Join("&", query);
        var url = "/api/reports/supplier-statistics/export" + (query.Any() ? $"?{queryString}" : "");
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<byte[]> ExportStocktakeStatisticsAsync(StocktakeStatisticsFilterModel filter)
    {
        var query = new List<string>();
        if (filter.FromDate.HasValue) query.Add($"fromDate={Uri.EscapeDataString(filter.FromDate.Value.ToString("O"))}");
        if (filter.ToDate.HasValue) query.Add($"toDate={Uri.EscapeDataString(filter.ToDate.Value.ToString("O"))}");
        if (!string.IsNullOrEmpty(filter.SessionStatus)) query.Add($"sessionStatus={Uri.EscapeDataString(filter.SessionStatus)}");

        var queryString = string.Join("&", query);
        var url = "/api/reports/stocktake-statistics/export" + (query.Any() ? $"?{queryString}" : "");
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<LowStockAlertResponseModel> GetLowStockAlertsAsync(LowStockAlertFilterModel filter)
    {
        var queryString = $"Keyword={Uri.EscapeDataString(filter.Keyword ?? "")}&PageNumber={filter.PageNumber}&PageSize={filter.PageSize}";
        var response = await _httpClient.GetAsync($"/api/reports/low-stock?{queryString}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LowStockAlertResponseModel>() ?? new LowStockAlertResponseModel();
    }

    public async Task<byte[]> ExportLowStockAlertsAsync(LowStockAlertFilterModel filter)
    {
        var queryString = $"Keyword={Uri.EscapeDataString(filter.Keyword ?? "")}&PageNumber={filter.PageNumber}&PageSize={filter.PageSize}";
        var response = await _httpClient.GetAsync($"/api/reports/low-stock/export?{queryString}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<ExpiringLotAlertResponseModel> GetExpiringLotAlertsAsync(ExpiringLotAlertFilterModel filter)
    {
        var queryString = $"Keyword={Uri.EscapeDataString(filter.Keyword ?? "")}&MaxDaysToExpiry={filter.MaxDaysToExpiry}&PageNumber={filter.PageNumber}&PageSize={filter.PageSize}";
        var response = await _httpClient.GetAsync($"/api/reports/expiring-lots?{queryString}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ExpiringLotAlertResponseModel>() ?? new ExpiringLotAlertResponseModel();
    }

    public async Task<byte[]> ExportExpiringLotAlertsAsync(ExpiringLotAlertFilterModel filter)
    {
        var queryString = $"Keyword={Uri.EscapeDataString(filter.Keyword ?? "")}&MaxDaysToExpiry={filter.MaxDaysToExpiry}&PageNumber={filter.PageNumber}&PageSize={filter.PageSize}";
        var response = await _httpClient.GetAsync($"/api/reports/expiring-lots/export?{queryString}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }
}
