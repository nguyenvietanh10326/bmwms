using System.Text.Json;
using BMWMS.Web.Models;

namespace BMWMS.Web.Services;

public class AuditLogApiService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AuditLogApiService(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("ApiClient");
    }

    public async Task<(AuditLogListResponseModel? Data, string? Error)> GetAuditLogsAsync(AuditLogFilterModel filter)
    {
        var query = new List<string>
        {
            $"pageIndex={filter.PageIndex}",
            $"pageSize={filter.PageSize}"
        };
        Add(query, "keyword", filter.Keyword);
        Add(query, "moduleCode", filter.ModuleCode);
        Add(query, "actionType", filter.ActionType);
        Add(query, "entityName", filter.EntityName);
        Add(query, "entityId", filter.EntityId);
        if (filter.UserId.HasValue) query.Add($"userId={filter.UserId.Value}");
        if (filter.FromDate.HasValue) query.Add($"fromDate={filter.FromDate:yyyy-MM-dd}");
        if (filter.ToDate.HasValue) query.Add($"toDate={filter.ToDate:yyyy-MM-dd}");

        return await GetAsync<AuditLogListResponseModel>($"api/audit-logs?{string.Join('&', query)}");
    }

    public Task<(AuditLogDetailModel? Data, string? Error)> GetAuditLogAsync(long id) =>
        GetAsync<AuditLogDetailModel>($"api/audit-logs/{id}");

    public Task<(AuditLogOptionsModel? Data, string? Error)> GetOptionsAsync() =>
        GetAsync<AuditLogOptionsModel>("api/audit-logs/options");

    private async Task<(T? Data, string? Error)> GetAsync<T>(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                return (JsonSerializer.Deserialize<T>(body, JsonOptions), null);

            return (default, ExtractError(body) ?? $"Không thể tải dữ liệu (HTTP {(int)response.StatusCode}).");
        }
        catch (TaskCanceledException)
        {
            return (default, "Yêu cầu tải nhật ký đã quá thời gian chờ.");
        }
        catch (HttpRequestException)
        {
            return (default, "Không thể kết nối đến dịch vụ nhật ký hoạt động.");
        }
    }

    private static void Add(ICollection<string> query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            query.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
    }

    private static string? ExtractError(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            return json.RootElement.TryGetProperty("message", out var message) ? message.GetString() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
