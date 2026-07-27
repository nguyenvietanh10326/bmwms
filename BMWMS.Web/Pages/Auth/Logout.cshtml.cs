using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Auth;

public class LogoutModel : PageModel
{
    private readonly BMWMS.Web.Services.AuthApiService _authApiService;

    public LogoutModel(BMWMS.Web.Services.AuthApiService authApiService)
    {
        _authApiService = authApiService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await ProcessLogout();
        return RedirectToPage("/Auth/Login");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await ProcessLogout();
        return RedirectToPage("/Auth/Login");
    }

    private async Task ProcessLogout()
    {
        var sessionId = HttpContext.Session.GetString("SessionId");
        var userId = HttpContext.Session.GetString("UserId");

        if (!string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(userId))
        {
            await _authApiService.LogoutAsync(sessionId, userId);
        }

        HttpContext.Session.Clear();
    }
}
