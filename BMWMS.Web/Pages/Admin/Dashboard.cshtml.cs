using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin;

public class DashboardModel : PageModel
{
    private readonly IDashboardApiService _dashboardApiService;

    public DashboardModel(IDashboardApiService dashboardApiService)
    {
        _dashboardApiService = dashboardApiService;
    }

    public string FullName { get; private set; } = "";
    public string RoleName { get; private set; } = "";
    public string RoleCode { get; private set; } = "";
    public string UserId { get; private set; } = "";
    public string LoginAt { get; private set; } = "";

    public DashboardResponseModel DashboardData { get; set; } = new DashboardResponseModel();

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (userId == null)
            return RedirectToPage("/Auth/Login");

        UserId   = userId;
        FullName = HttpContext.Session.GetString("FullName") ?? "";
        RoleName = HttpContext.Session.GetString("RoleName") ?? "";
        RoleCode = HttpContext.Session.GetString("RoleCode") ?? "";
        LoginAt  = HttpContext.Session.GetString("LoginAt")  ?? "";

        var data = await _dashboardApiService.GetDashboardDataAsync();
        if (data != null)
        {
            DashboardData = data;
        }

        return Page();
    }
}
