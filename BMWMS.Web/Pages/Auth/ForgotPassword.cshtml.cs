using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BMWMS.Web.Services;

namespace BMWMS.Web.Pages.Auth;

public class ForgotPasswordModel : PageModel
{
    private readonly AuthApiService _authApiService;

    public ForgotPasswordModel(AuthApiService authApiService)
    {
        _authApiService = authApiService;
    }

    [BindProperty]
    public string Email { get; set; } = "";

    public string? ErrorMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var error = await _authApiService.ForgotPasswordAsync(Email);
        
        if (error != null)
        {
            ErrorMessage = error;
            return Page();
        }

        // Redirect to Reset Password with email
        TempData["ResetEmail"] = Email;
        return RedirectToPage("/Auth/ResetPassword");
    }
}
