using System.Threading.Tasks;
using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Users;

public class ChangeLockStateModelPage : PageModel
{
    private readonly UserApiService _userApiService;

    public ChangeLockStateModelPage(UserApiService userApiService)
    {
        _userApiService = userApiService;
    }

    [BindProperty]
    public ChangeLockStateModel Input { get; set; } = new();

    public string DisplayUser { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public string DisplayAction { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var userDetail = await _userApiService.GetUserDetailAsync(id);
        if (userDetail == null)
        {
            return RedirectToPage("./Index");
        }

        if (userDetail.Status == "INACTIVE")
        {
            TempData["ErrorMessage"] = "Không thể khóa/mở khóa tài khoản Không hoạt động.";
            return RedirectToPage("./Detail", new { id });
        }

        var isLocked = userDetail.Status == "LOCKED";

        Input = new ChangeLockStateModel
        {
            UserId = userDetail.UserId,
            Action = isLocked ? "UNLOCK" : "LOCK"
        };

        DisplayUser = $"{userDetail.UserCode} — {userDetail.FullName} ({userDetail.Username})";
        CurrentStatus = isLocked ? "Đã khóa" : "Đang hoạt động";
        DisplayAction = isLocked ? "Mở khóa tài khoản" : "Khóa tài khoản";

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
            return Page();
        }

        var (success, message) = await _userApiService.ChangeUserLockStateAsync(id, Input);

        if (success)
        {
            TempData["SuccessMessage"] = message;
            return RedirectToPage("./Detail", new { id = Input.UserId });
        }

        ModelState.AddModelError(string.Empty, message);
        await ReloadDisplayDataAsync(id);
        return Page();
    }

    private async Task ReloadDisplayDataAsync(long id)
    {
        var userDetail = await _userApiService.GetUserDetailAsync(id);
        if (userDetail != null)
        {
            DisplayUser = $"{userDetail.UserCode} — {userDetail.FullName} ({userDetail.Username})";
            CurrentStatus = userDetail.Status == "LOCKED" ? "Đã khóa" : "Đang hoạt động";
            DisplayAction = userDetail.Status == "LOCKED" ? "Mở khóa tài khoản" : "Khóa tài khoản";
        }
    }
}
