using BMWMS.Web.Models;
using BMWMS.Web.Services;
using BMWMS.Web.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.AuditLogs;

public class DetailsModel : PageModel
{
    private readonly AuditLogApiService _auditLogApiService;

    public DetailsModel(AuditLogApiService auditLogApiService)
    {
        _auditLogApiService = auditLogApiService;
    }

    public AuditLogDetailModel? AuditLog { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        if (HttpContext.Session.GetString("RoleCode") != "SYSTEM_ADMIN")
            return RedirectToPage("/Admin/Dashboard");

        var result = await _auditLogApiService.GetAuditLogAsync(id);
        AuditLog = result.Data;
        ErrorMessage = result.Error;
        if (AuditLog is null && string.IsNullOrWhiteSpace(ErrorMessage)) return NotFound();
        return Page();
    }

    public string FormatLocalTime(DateTime utc) =>
        VietnamTime.Format(utc);

    public string DisplayValue(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;

    public string ChangeTypeLabel(string changeType) => changeType switch
    {
        "ADDED" => "Bổ sung",
        "REMOVED" => "Loại bỏ",
        _ => "Thay đổi"
    };
}
