using System.Text.Json;
using BMWMS.Web.Models;

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
            var response = await _httpClient.GetAsync("/api/dashboard");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<DashboardResponseModel>(content, _jsonOptions);
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
