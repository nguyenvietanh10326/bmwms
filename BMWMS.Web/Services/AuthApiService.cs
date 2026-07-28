using System.Net;
using System.Text;
using System.Text.Json;
using BMWMS.Web.Models;

namespace BMWMS.Web.Services;

public class AuthApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthApiService> _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthApiService(IHttpClientFactory factory, ILogger<AuthApiService> logger)
    {
        _httpClient = factory.CreateClient("ApiClient");
        _logger = logger;
    }

    public record LoginResult(
        bool Success,
        UserSessionInfo? User,
        string? ErrorMessage,
        string? ErrorType,        // "invalid" | "locked" | "inactive" | "server"
        int? LockoutRemaining
    );

    public async Task<LoginResult> LoginAsync(string usernameOrEmail, string password)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                UsernameOrEmail = usernameOrEmail,
                Password = password
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/auth/login", content);

            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var data = JsonSerializer.Deserialize<UserSessionInfo>(body, _json);
                return new LoginResult(true, data, null, null, null);
            }

            // HTTP 423 → account locked
            if ((int)response.StatusCode == 423)
            {
                var error = JsonSerializer.Deserialize<JsonElement>(body, _json);
                int remaining = error.TryGetProperty("remainingMinutes", out var rm) ? rm.GetInt32() : 15;
                return new LoginResult(false, null,
                    $"Tài khoản bị khóa tạm thời. Vui lòng thử lại sau {remaining} phút.",
                    "locked", remaining);
            }

            // HTTP 401 → invalid credentials or inactive
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var error = JsonSerializer.Deserialize<JsonElement>(body, _json);
                var msg = error.TryGetProperty("message", out var m) ? m.GetString() : "Đăng nhập thất bại.";

                if (msg != null && msg.Contains("vô hiệu hóa"))
                    return new LoginResult(false, null, msg, "inactive", null);

                return new LoginResult(false, null,
                    "Tên đăng nhập hoặc mật khẩu không đúng.", "invalid", null);
            }

            _logger.LogWarning("Login API returned {Status}", response.StatusCode);
            return new LoginResult(false, null, "Lỗi hệ thống. Vui lòng thử lại.", "server", null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Không kết nối được API khi đăng nhập");
            return new LoginResult(false, null, "Không thể kết nối đến máy chủ. Vui lòng thử lại.", "server", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi LoginAsync");
            return new LoginResult(false, null, "Lỗi không xác định.", "server", null);
        }
    }

    public async Task LogoutAsync(string sessionId, string userId)
    {
        try
        {
            await _httpClient.PostAsync("api/auth/logout", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi Logout API");
        }
    }

    public async Task<string?> ForgotPasswordAsync(string email)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new { Email = email });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/auth/forgot-password", content);

            if (response.IsSuccessStatusCode)
                return null; // success

            var body = await response.Content.ReadAsStringAsync();
            var error = JsonSerializer.Deserialize<JsonElement>(body, _json);
            return error.TryGetProperty("message", out var m) ? m.GetString() : "Có lỗi xảy ra.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi ForgotPassword");
            return "Không thể kết nối đến máy chủ.";
        }
    }

    public async Task<string?> ResetPasswordAsync(string email, string token, string newPassword, string confirmNewPassword)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new 
            { 
                Email = email,
                Token = token,
                NewPassword = newPassword,
                ConfirmNewPassword = confirmNewPassword
            });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/auth/reset-password", content);

            if (response.IsSuccessStatusCode)
                return null; // success

            var body = await response.Content.ReadAsStringAsync();
            var error = JsonSerializer.Deserialize<JsonElement>(body, _json);
            return error.TryGetProperty("message", out var m) ? m.GetString() : "Có lỗi xảy ra.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi ResetPassword");
            return "Không thể kết nối đến máy chủ.";
        }
    }
}

/// <summary>Thông tin user lưu vào Session sau login</summary>
public class UserSessionInfo
{
    public long UserId { get; set; }
    public string Username { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string RoleCode { get; set; } = null!;
    public string RoleName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTime LoginAt { get; set; }
    public Guid SessionId { get; set; }
    public string? Token { get; set; }
}
