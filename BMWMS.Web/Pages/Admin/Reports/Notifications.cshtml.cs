using System.Threading.Tasks;
using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Reports;

public class NotificationsModel : PageModel
{
    private readonly INotificationApiService _notificationApiService;

    public NotificationsModel(INotificationApiService notificationApiService)
    {
        _notificationApiService = notificationApiService;
    }

    [BindProperty(SupportsGet = true)]
    public NotificationFilterModel Filter { get; set; } = new();

    public NotificationResponseModel ReportData { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        ReportData = await _notificationApiService.GetNotificationsAsync(Filter);
        return Page();
    }

    public async Task<IActionResult> OnPostMarkAsReadAsync(long id)
    {
        await _notificationApiService.MarkAsReadAsync(id);
        return RedirectToPage(new { Filter.IsRead, Filter.PageNumber, Filter.PageSize });
    }

    public async Task<IActionResult> OnPostMarkAllAsReadAsync()
    {
        await _notificationApiService.MarkAllAsReadAsync();
        return RedirectToPage(new { Filter.IsRead, Filter.PageNumber, Filter.PageSize });
    }
}
