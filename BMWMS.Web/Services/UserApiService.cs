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
        var request = new HttpRequestMessage(HttpMethod.Get, "api/user/me");
        request.Headers.Add("X-User-Id", userId.ToString());

        var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserSessionInfo>(body, _json);
        }
        return null;
    }

    public async Task<(bool Success, string Message)> UpdateProfileAsync(long userId, string fullName, string? phoneNumber, string? email)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, "api/user/me/profile");
        request.Headers.Add("X-User-Id", userId.ToString());

        var payload = JsonSerializer.Serialize(new
        {
            FullName = fullName,
            PhoneNumber = phoneNumber,
            Email = email
        });
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
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
        var request = new HttpRequestMessage(HttpMethod.Put, "api/user/me/password");
        request.Headers.Add("X-User-Id", userId.ToString());

        var payload = JsonSerializer.Serialize(new
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword,
            ConfirmNewPassword = confirmNewPassword,
            SignOutOtherSessions = signOutOtherSessions
        });
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
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
}
