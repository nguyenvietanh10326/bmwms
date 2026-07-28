using System.Collections.Generic;
using System.Threading.Tasks;
using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMWMS.Web.Pages.Admin.Users;

public class CreateModel : PageModel
{
    private readonly UserApiService _userApiService;
    private readonly RoleApiService _roleApiService;

    public CreateModel(UserApiService userApiService, RoleApiService roleApiService)
    {
        _userApiService = userApiService;
        _roleApiService = roleApiService;
    }

    [BindProperty]
    public CreateUserModel Input { get; set; } = new();

    public List<SelectListItem> Roles { get; set; } = new();

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

        var (success, message) = await _userApiService.CreateUserAsync(Input);

        if (success)
        {
            TempData["SuccessMessage"] = message;
            return RedirectToPage("./Index");
        }

        ModelState.AddModelError(string.Empty, message);
        await LoadRolesAsync();
        return Page();
    }

    private async Task LoadRolesAsync()
    {
        var roles = await _roleApiService.GetActiveRolesAsync();
        foreach (var role in roles)
        {
            Roles.Add(new SelectListItem { Value = role.RoleId.ToString(), Text = role.RoleName });
        }
    }
}
