using System.Text.Json;
using BMWMS.Web.Models;
using BMWMS.Web.Models.Inventory;

namespace BMWMS.Web.Services;

public class DashboardApiService : IDashboardApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DashboardApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public DashboardApiService(IHttpClientFactory httpClientFactory, ILogger<DashboardApiService> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<DashboardResponseModel?> GetDashboardDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/dashboard/summary");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var summary = JsonSerializer.Deserialize<DashboardSummaryDto>(content, _jsonOptions);
                if (summary == null)
                    return null;

                return new DashboardResponseModel
                {
                    TotalProducts = summary.TotalProducts,
                    NewProductsThisMonth = 0,
                    ActiveWarehouses = summary.ActiveWarehouses,
                    LowStockCount = summary.LowStockCount,
                    OutOfStockCount = summary.OutOfStockCount,
                    PendingPurchaseOrders = summary.PendingPurchaseOrders,
                    PendingSalesOrders = summary.PendingSalesOrders,
                    ProcessingInboundOrders = summary.ProcessingInboundOrders,
                    PickingOutboundOrders = summary.PickingOutboundOrders,
                    InventoryAlerts = summary.LowStockAlerts.Select(x => new DashboardAlertModel
                    {
                        ProductName = $"{x.ProductCode} - {x.ProductName}",
                        AvailableQuantity = x.AvailableQuantity,
                        Threshold = x.Threshold,
                        Status = x.Status,
                        Unit = x.UnitName
                    }).ToList(),
                    RecentActivities = summary.RecentActivities.Select(x => new DashboardActivityModel
                    {
                        ActivityText = x.Title,
                        PerformedBy = x.PerformerName,
                        CreatedAt = DateTime.Now,
                        ColorType = x.StatusType == "Success" ? "blue" : "orange"
                    }).ToList()
                };
            }
            
            _logger.LogWarning("Failed to get dashboard data. Status code: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching dashboard data");
            return null;
        }
    }
}
