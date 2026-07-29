using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BMWMS.Web.Services;

namespace BMWMS.Web.Pages.Auth;

public class ResetPasswordModel : PageModel
{
    private readonly AuthApiService _authApiService;

    public ResetPasswordModel(AuthApiService authApiService)
    {
        _authApiService = authApiService;
    }

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Token { get; set; } = "";

    [BindProperty]
    public string NewPassword { get; set; } = "";

    [BindProperty]
    public string ConfirmNewPassword { get; set; } = "";

    public string? ErrorMessage { get; set; }
    
    [TempData]
    public string? SuccessMessage { get; set; }

    public IActionResult OnGet()
    {
        if (TempData.TryGetValue("ResetEmail", out var emailObj) && emailObj is string email)
        {
            Email = email;
            // keep it in temp data in case they refresh or get error
            TempData.Keep("ResetEmail"); 
            return Page();
        }

        // If no email, redirect to forgot password
        return RedirectToPage("/Auth/ForgotPassword");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        TempData.Keep("ResetEmail");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (NewPassword != ConfirmNewPassword)
        {
            ErrorMessage = "Xác nhận mật khẩu không khớp.";
            return Page();
        }

        var error = await _authApiService.ResetPasswordAsync(Email, Token, NewPassword, ConfirmNewPassword);
        
        if (error != null)
        {
            ErrorMessage = error;
            return Page();
        }

        SuccessMessage = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại với mật khẩu mới.";
        return RedirectToPage("/Auth/Login");
    }
}
