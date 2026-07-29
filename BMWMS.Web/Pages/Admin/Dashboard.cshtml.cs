using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin;

public class DashboardModel : PageModel
{
    public string FullName { get; private set; } = "";
    public string RoleName { get; private set; } = "";
    public string RoleCode { get; private set; } = "";
    public string UserId { get; private set; } = "";
    public string LoginAt { get; private set; } = "";

    public IActionResult OnGet()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null)
            return RedirectToPage("/Auth/Login");

        UserId   = userId;
        FullName = HttpContext.Session.GetString("FullName") ?? "";
        RoleName = HttpContext.Session.GetString("RoleName") ?? "";
        RoleCode = HttpContext.Session.GetString("RoleCode") ?? "";
        LoginAt  = HttpContext.Session.GetString("LoginAt")  ?? "";

        return Page();
    }
}
