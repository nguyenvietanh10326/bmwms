using BMWMS.Web.Models;
using BMWMS.Web.Services;
using BMWMS.Web.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Users;

public class AuditLogsModel : PageModel
{
    private readonly AuditLogApiService _auditLogApiService;

    public AuditLogsModel(AuditLogApiService auditLogApiService)
    {
        _auditLogApiService = auditLogApiService;
    }

    [BindProperty(SupportsGet = true)]
    public AuditLogFilterModel Filter { get; set; } = new();

    public PagedResult<AuditLogListItemModel> AuditLogs { get; set; } = new();
    public AuditLogOptionsModel Options { get; set; } = new();
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (HttpContext.Session.GetString("RoleCode") != "SYSTEM_ADMIN")
            return RedirectToPage("/Admin/Dashboard");

        // Force EntityName = "User" and ModuleCode = "ACCOUNT" to only fetch user logs
        Filter.EntityName = "User";
        Filter.ModuleCode = "ACCOUNT";
        
        Filter.PageIndex = Math.Max(1, Filter.PageIndex);
        Filter.PageSize = Math.Clamp(Filter.PageSize, 10, 100);
        if (Filter.FromDate.HasValue && Filter.ToDate.HasValue && Filter.FromDate > Filter.ToDate)
        {
            ErrorMessage = "Ngày bắt đầu không được sau ngày kết thúc.";
            return Page();
        }

        var logsTask = _auditLogApiService.GetAuditLogsAsync(Filter);
        var optionsTask = _auditLogApiService.GetOptionsAsync("User");
        await Task.WhenAll(logsTask, optionsTask);

        var logsResult = logsTask.Result;
        var optionsResult = optionsTask.Result;
        AuditLogs = logsResult.Data?.Data ?? new PagedResult<AuditLogListItemModel>
        {
            PageIndex = Filter.PageIndex,
            PageSize = Filter.PageSize
        };
        Options = optionsResult.Data ?? new AuditLogOptionsModel();

        ErrorMessage = logsResult.Error ?? optionsResult.Error;
        return Page();
    }

    public string BuildPageUrl(int pageIndex)
    {
        var query = new List<string>
        {
            $"Filter.PageIndex={pageIndex}",
            $"Filter.PageSize={Filter.PageSize}"
        };
        Add(query, "Filter.Keyword", Filter.Keyword);
        Add(query, "Filter.ActionType", Filter.ActionType);
        // Do not add EntityName/ModuleCode to URL as they are forced in backend
        if (Filter.UserId.HasValue) query.Add($"Filter.UserId={Filter.UserId.Value}");
        if (Filter.FromDate.HasValue) query.Add($"Filter.FromDate={Filter.FromDate:yyyy-MM-dd}");
        if (Filter.ToDate.HasValue) query.Add($"Filter.ToDate={Filter.ToDate:yyyy-MM-dd}");
        return $"/Admin/Users/AuditLogs?{string.Join('&', query)}";
    }

    public string FormatLocalTime(DateTime utc) =>
        VietnamTime.Format(utc);

    private static void Add(ICollection<string> query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            query.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
    }
}
