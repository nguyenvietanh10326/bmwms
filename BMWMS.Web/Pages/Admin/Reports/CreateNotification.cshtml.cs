using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMWMS.Web.Pages.Admin.Reports;

public class CreateNotificationModel : PageModel
{
    private readonly INotificationApiService _notificationService;
    private readonly RoleApiService _roleApiService;

    public CreateNotificationModel(INotificationApiService notificationService, RoleApiService roleApiService)
    {
        _notificationService = notificationService;
        _roleApiService = roleApiService;
    }

    [BindProperty]
    public BMWMS.Web.Models.CreateNotificationModel Input { get; set; } = new();

    public List<SelectListItem> RoleOptions { get; set; } = new();
    
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadRolesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadRolesAsync();
            return Page();
        }

        try
        {
            await _notificationService.CreateNotificationAsync(Input);
            TempData["SuccessMessage"] = "Đã gửi thông báo thành công.";
            return RedirectToPage("/Admin/Reports/Notifications");
        }
        catch (Exception ex)
        {
            ErrorMessage = "Đã xảy ra lỗi khi tạo thông báo.";
            await LoadRolesAsync();
            return Page();
        }
    }

    private async Task LoadRolesAsync()
    {
        var roles = await _roleApiService.GetActiveRolesAsync();
        RoleOptions = roles.Select(r => new SelectListItem
        {
            Value = r.RoleId.ToString(),
            Text = r.RoleName
        }).ToList();
    }
}
