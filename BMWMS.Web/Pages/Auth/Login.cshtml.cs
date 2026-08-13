using System.Security.Claims;
using System.Text.Json;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly AuthApiService _authApiService;

    public LoginModel(AuthApiService authApiService)
    {
        _authApiService = authApiService;
    }

    [BindProperty]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? ErrorType { get; set; }
    public int? LockoutRemainingMinutes { get; set; }

    public IActionResult OnGet()
    {
        // Nếu đã đăng nhập thì redirect về Dashboard
        if (HttpContext.Session.GetString("UserId") != null)
            return RedirectToPage("/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(UsernameOrEmail) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Vui lòng nhập đầy đủ thông tin.";
            ErrorType = "invalid";
            return Page();
        }

        var result = await _authApiService.LoginAsync(UsernameOrEmail.Trim(), Password);

        if (!result.Success)
        {
            ErrorMessage = result.ErrorMessage;
            ErrorType = result.ErrorType;
            LockoutRemainingMinutes = result.LockoutRemaining;
            // Giữ lại username (không giữ password)
            return Page();
        }

        // Đăng nhập thành công → lưu vào Session
        var user = result.User!;
        HttpContext.Session.SetString("UserId",    user.UserId.ToString());
        HttpContext.Session.SetString("Username",  user.Username);
        HttpContext.Session.SetString("FullName",  user.FullName);
        HttpContext.Session.SetString("Email",     user.Email);
        HttpContext.Session.SetString("RoleCode",  user.RoleCode);
        HttpContext.Session.SetString("RoleName",  user.RoleName);
        HttpContext.Session.SetString("LoginAt",   user.LoginAt.ToString("o"));
        HttpContext.Session.SetString("SessionId", user.SessionId.ToString());
        if (!string.IsNullOrEmpty(user.AvatarUrl))
            HttpContext.Session.SetString("AvatarUrl", user.AvatarUrl);
        if (!string.IsNullOrEmpty(user.Token))
            HttpContext.Session.SetString("Token", user.Token);

        // Sign in with Cookie Authentication
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.RoleCode),
            new Claim("FullName", user.FullName),
            new Claim("SessionId", user.SessionId.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, 
            new ClaimsPrincipal(claimsIdentity), 
            authProperties);

        // Tất cả Role đều vào Dashboard chung
        return RedirectToPage("/Admin/Dashboard");
    }
}
