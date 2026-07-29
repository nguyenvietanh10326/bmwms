using System.Collections.Generic;
using System.Threading.Tasks;
using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMWMS.Web.Pages.Admin.Users;

public class EditModel : PageModel
{
    private readonly UserApiService _userApiService;
    private readonly RoleApiService _roleApiService;

    public EditModel(UserApiService userApiService, RoleApiService roleApiService)
    {
        _userApiService = userApiService;
        _roleApiService = roleApiService;
    }

    [BindProperty]
    public UpdateUserModel Input { get; set; } = new();

    public string Username { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;

    public List<SelectListItem> Roles { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var userDetail = await _userApiService.GetUserDetailAsync(id);
        if (userDetail == null)
        {
            return RedirectToPage("./Index");
        }

        Input = new UpdateUserModel
        {
            UserId = userDetail.UserId,
            FullName = userDetail.FullName,
            Email = userDetail.Email,
            PhoneNumber = userDetail.PhoneNumber,
            Status = userDetail.Status
        };

        Username = userDetail.Username;
        UserCode = userDetail.UserCode;

        await LoadRolesAsync(userDetail.RoleCode);
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
            await LoadRolesAsync();
            return Page();
        }

        var (success, message) = await _userApiService.UpdateUserAsync(id, Input);

        if (success)
        {
            TempData["SuccessMessage"] = message;
            return RedirectToPage("./Detail", new { id = Input.UserId });
        }

        ModelState.AddModelError(string.Empty, message);
        await LoadRolesAsync();
        return Page();
    }

    private async Task LoadRolesAsync(string? currentRoleCode = null)
    {
        var roles = await _roleApiService.GetActiveRolesAsync();
        foreach (var role in roles)
        {
            Roles.Add(new SelectListItem { Value = role.RoleId.ToString(), Text = role.RoleName });
            
            if (currentRoleCode != null && role.RoleCode == currentRoleCode)
            {
                Input.RoleId = role.RoleId;
            }
        }
    }
}
