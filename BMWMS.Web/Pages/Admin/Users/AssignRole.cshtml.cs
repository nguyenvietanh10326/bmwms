using System.Collections.Generic;
using System.Threading.Tasks;
using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMWMS.Web.Pages.Admin.Users;

public class AssignRoleModelPage : PageModel
{
    private readonly UserApiService _userApiService;
    private readonly RoleApiService _roleApiService;

    public AssignRoleModelPage(UserApiService userApiService, RoleApiService roleApiService)
    {
        _userApiService = userApiService;
        _roleApiService = roleApiService;
    }

    [BindProperty]
    public AssignRoleModel Input { get; set; } = new();

    public string DisplayUser { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = string.Empty;

    public List<SelectListItem> Roles { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var userDetail = await _userApiService.GetUserDetailAsync(id);
        if (userDetail == null)
        {
            return RedirectToPage("./Index");
        }

        Input = new AssignRoleModel
        {
            UserId = userDetail.UserId,
            RoleId = 0 // Bắt buộc user phải chọn Role mới
        };

        DisplayUser = $"{userDetail.UserCode} — {userDetail.FullName}";
        CurrentRole = userDetail.RoleName;

        await LoadRolesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id)
    {
        if (id != Input.UserId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await ReloadDisplayDataAsync(id);
            await LoadRolesAsync();
            return Page();
        }

        var (success, message) = await _userApiService.AssignRoleAsync(id, Input);

        if (success)
        {
            TempData["SuccessMessage"] = message;
            return RedirectToPage("./Detail", new { id = Input.UserId });
        }

        ModelState.AddModelError(string.Empty, message);
        await ReloadDisplayDataAsync(id);
        await LoadRolesAsync();
        return Page();
    }

    private async Task LoadRolesAsync()
    {
        Roles.Clear();
        var roles = await _roleApiService.GetActiveRolesAsync();
        foreach (var role in roles)
        {
            Roles.Add(new SelectListItem { Value = role.RoleId.ToString(), Text = role.RoleName });
        }
    }

    private async Task ReloadDisplayDataAsync(long id)
    {
        var userDetail = await _userApiService.GetUserDetailAsync(id);
        if (userDetail != null)
        {
            DisplayUser = $"{userDetail.UserCode} — {userDetail.FullName}";
            CurrentRole = userDetail.RoleName;
        }
    }
}
