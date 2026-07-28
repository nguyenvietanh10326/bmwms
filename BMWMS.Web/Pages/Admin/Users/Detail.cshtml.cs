using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BMWMS.Web.Services;
using BMWMS.Web.Models;

namespace BMWMS.Web.Pages.Admin.Users;

public class DetailModel : PageModel
{
    private readonly UserApiService _userApiService;

    public DetailModel(UserApiService userApiService)
    {
        _userApiService = userApiService;
    }

    public UserDetailModel UserDetail { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var userDetail = await _userApiService.GetUserDetailAsync(id);
        if (userDetail == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy người dùng hoặc người dùng không tồn tại.";
            return RedirectToPage("/Admin/Users/Index");
        }

        UserDetail = userDetail;
        return Page();
    }
}
