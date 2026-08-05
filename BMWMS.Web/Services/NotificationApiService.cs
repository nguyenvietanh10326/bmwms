using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BMWMS.Web.Models;

namespace BMWMS.Web.Services;

public class NotificationApiService : INotificationApiService
{
    private readonly HttpClient _httpClient;

    public NotificationApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
    }

    public async Task<NotificationResponseModel> GetNotificationsAsync(NotificationFilterModel filter)
    {
        var isReadParam = filter.IsRead.HasValue ? $"&IsRead={filter.IsRead.Value}" : "";
        var url = $"/api/notifications?PageNumber={filter.PageNumber}&PageSize={filter.PageSize}{isReadParam}";
        
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<NotificationResponseModel>() ?? new NotificationResponseModel();
    }

    public async Task MarkAsReadAsync(long notificationId)
    {
        var response = await _httpClient.PutAsync($"/api/notifications/{notificationId}/read", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task MarkAllAsReadAsync()
    {
        var response = await _httpClient.PutAsync("/api/notifications/read-all", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task CreateNotificationAsync(CreateNotificationModel model)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/notifications", model);
        response.EnsureSuccessStatusCode();
    }
}
