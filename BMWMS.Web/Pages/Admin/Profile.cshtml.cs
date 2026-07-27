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

    public IActionResult OnGet()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null)
            return RedirectToPage("/Auth/Login");

        UserId = userId;
        FullName = HttpContext.Session.GetString("FullName") ?? "";
        RoleName = HttpContext.Session.GetString("RoleName") ?? "";
        RoleCode = HttpContext.Session.GetString("RoleCode") ?? "";
        LoginAt = HttpContext.Session.GetString("LoginAt") ?? "";
        
        // For the static display, we simulate these for now
        Email = HttpContext.Session.GetString("Email") ?? "admin@bmwms.local";
        PhoneNumber = "0900000000";
        Username = "admin";
        Status = "ACTIVE";

        return Page();
    }
}
