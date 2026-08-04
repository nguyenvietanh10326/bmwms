using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BMWMS.Web.Services;
using BMWMS.Web.Models;

namespace BMWMS.Web.Pages.Admin.Users;

public class IndexModel : PageModel
{
    private readonly UserApiService _userApiService;

    public IndexModel(UserApiService userApiService)
    {
        _userApiService = userApiService;
    }

    [BindProperty(SupportsGet = true)]
    public UserFilterModel Filter { get; set; } = new();

    public PagedResult<UserListItem>? UserList { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var roleCode = HttpContext.Session.GetString("RoleCode");
        if (roleCode != "SYSTEM_ADMIN")
        {
            return RedirectToPage("/Admin/Dashboard");
        }

        if (Filter.PageIndex < 1) Filter.PageIndex = 1;
        if (Filter.PageSize < 1) Filter.PageSize = 10;

        UserList = await _userApiService.GetUsersAsync(Filter);

        return Page();
    }
}
