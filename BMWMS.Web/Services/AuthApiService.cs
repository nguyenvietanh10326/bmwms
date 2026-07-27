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
            var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
            request.Headers.Add("X-Session-Id", sessionId);
            request.Headers.Add("X-User-Id", userId);

            await _httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi Logout API");
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
}
