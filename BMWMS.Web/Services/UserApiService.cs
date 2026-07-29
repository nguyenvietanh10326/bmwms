using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BMWMS.Web.Services;

public class UserApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserApiService> _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public UserApiService(IHttpClientFactory factory, ILogger<UserApiService> logger)
    {
        _httpClient = factory.CreateClient("ApiClient");
        _logger = logger;
    }

    public async Task<UserSessionInfo?> GetProfileAsync(long userId)
    {
        var response = await _httpClient.GetAsync("api/user/me");
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserSessionInfo>(body, _json);
        }
        return null;
    }

    public async Task<(bool Success, string Message)> UpdateProfileAsync(long userId, string fullName, string? phoneNumber, string? email)
    {
        var payload = JsonSerializer.Serialize(new
        {
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Email = email
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync("api/user/me/profile", content);
        var body = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return (true, "Cập nhật hồ sơ thành công");
        }

        var errorMsg = ExtractErrorMessage(body);
        return (false, errorMsg);
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(long userId, string currentPassword, string newPassword, string confirmNewPassword, bool signOutOtherSessions)
    {
        var payload = JsonSerializer.Serialize(new
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword,
            ConfirmNewPassword = confirmNewPassword,
            SignOutOtherSessions = signOutOtherSessions
        });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PutAsync("api/user/me/password", content); 
        var body = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return (true, "Đổi mật khẩu thành công");
        }

        var errorMsg = ExtractErrorMessage(body);
        return (false, errorMsg);
    }

    private string ExtractErrorMessage(string body)
    {
        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msg))
            {
                return msg.GetString() ?? "Đã có lỗi xảy ra.";
            }
        }
        catch { }
        return "Đã có lỗi xảy ra từ máy chủ.";
    }
    public async Task<BMWMS.Web.Models.PagedResult<BMWMS.Web.Models.UserListItem>?> GetUsersAsync(BMWMS.Web.Models.UserFilterModel filter)
    {
        var queryString = $"?pageIndex={filter.PageIndex}&pageSize={filter.PageSize}";
        if (!string.IsNullOrWhiteSpace(filter.Keyword)) queryString += $"&keyword={Uri.EscapeDataString(filter.Keyword)}";
        if (!string.IsNullOrWhiteSpace(filter.RoleCode)) queryString += $"&roleCode={Uri.EscapeDataString(filter.RoleCode)}";
        if (!string.IsNullOrWhiteSpace(filter.Status)) queryString += $"&status={Uri.EscapeDataString(filter.Status)}";

        var response = await _httpClient.GetAsync($"api/user{queryString}");
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BMWMS.Web.Models.PagedResult<BMWMS.Web.Models.UserListItem>>(body, _json);
        }
        return null;
    }

    public async Task<BMWMS.Web.Models.UserDetailModel?> GetUserDetailAsync(long id)
    {
        var response = await _httpClient.GetAsync($"api/user/{id}");
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BMWMS.Web.Models.UserDetailModel>(body, _json);
        }
        return null;
    }

    public async Task<(bool Success, string Message)> CreateUserAsync(BMWMS.Web.Models.CreateUserModel model)
    {
        var payload = JsonSerializer.Serialize(model);
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("api/user", content);
        var body = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return (true, "Tạo người dùng thành công");
        }

        var errorMsg = ExtractErrorMessage(body);
        return (false, errorMsg);
    }
}
