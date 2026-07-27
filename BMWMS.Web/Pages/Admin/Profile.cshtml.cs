using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin;

public class ProfileModel : PageModel
{
    public string FullName { get; private set; } = "";
    public string RoleName { get; private set; } = "";
    public string RoleCode { get; private set; } = "";
    public string UserId { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string PhoneNumber { get; private set; } = "";
    public string Username { get; private set; } = "";
    public string Status { get; private set; } = "";
    public string LoginAt { get; private set; } = "";

    [BindProperty]
    public string UpdateFullName { get; set; } = "";
    
    [BindProperty]
    public string UpdatePhoneNumber { get; set; } = "";
    
    [BindProperty]
    public string UpdateEmail { get; set; } = "";

    [BindProperty]
    public string CurrentPassword { get; set; } = "";
    
    [BindProperty]
    public string NewPassword { get; set; } = "";
    
    [BindProperty]
    public string ConfirmNewPassword { get; set; } = "";

    [BindProperty]
    public bool SignOutOtherSessions { get; set; } = true;

    [TempData]
    public string? SuccessMessage { get; set; }
    
    [TempData]
    public string? ErrorMessage { get; set; }

    private readonly BMWMS.Web.Services.UserApiService _userApiService;

    public ProfileModel(BMWMS.Web.Services.UserApiService userApiService)
    {
        _userApiService = userApiService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
            return RedirectToPage("/Auth/Login");

        var profile = await _userApiService.GetProfileAsync(userId);
        if (profile == null)
        {
            return RedirectToPage("/Auth/Login");
        }

        UserId = profile.UserId.ToString();
        FullName = profile.FullName;
        RoleName = profile.RoleName;
        RoleCode = profile.RoleCode;
        LoginAt = profile.LoginAt.ToString("o");
        Email = profile.Email;
        PhoneNumber = profile.PhoneNumber ?? "";
        Username = profile.Username;
        Status = "ACTIVE";

        UpdateFullName = FullName;
        UpdateEmail = Email;
        UpdatePhoneNumber = PhoneNumber;

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
            return RedirectToPage("/Auth/Login");

        var result = await _userApiService.UpdateProfileAsync(userId, UpdateFullName, UpdatePhoneNumber, UpdateEmail);
        
        if (result.Success)
        {
            SuccessMessage = result.Message;
            HttpContext.Session.SetString("FullName", UpdateFullName);
            HttpContext.Session.SetString("Email", UpdateEmail);
            return RedirectToPage(new { tab = "profile" });
        }
        else
        {
            ErrorMessage = result.Message;
            return RedirectToPage(new { tab = "profile" });
        }
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (userIdStr == null || !long.TryParse(userIdStr, out var userId))
            return RedirectToPage("/Auth/Login");

        var result = await _userApiService.ChangePasswordAsync(userId, CurrentPassword, NewPassword, ConfirmNewPassword, SignOutOtherSessions);

        if (result.Success)
        {
            SuccessMessage = result.Message;
            return RedirectToPage(new { tab = "password" });
        }
        else
        {
            ErrorMessage = result.Message;
            return RedirectToPage(new { tab = "password" });
        }
    }
}
