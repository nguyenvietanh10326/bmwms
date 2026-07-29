using System.Text.Json;
using BMWMS.Web.Services;
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

        // Tất cả Role đều vào Dashboard chung
        return RedirectToPage("/Admin/Dashboard");
    }
}
