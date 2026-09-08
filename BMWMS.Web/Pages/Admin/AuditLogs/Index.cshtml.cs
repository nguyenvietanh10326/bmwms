using BMWMS.Web.Models;
using BMWMS.Web.Services;
using BMWMS.Web.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.AuditLogs;

public class IndexModel : PageModel
{
    private readonly AuditLogApiService _auditLogApiService;

    public IndexModel(AuditLogApiService auditLogApiService)
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

        Filter.PageIndex = Math.Max(1, Filter.PageIndex);
        Filter.PageSize = Math.Clamp(Filter.PageSize, 10, 100);
        if (Filter.FromDate.HasValue && Filter.ToDate.HasValue && Filter.FromDate > Filter.ToDate)
        {
            ErrorMessage = "Ngày bắt đầu không được sau ngày kết thúc.";
            return Page();
        }

        var logsTask = _auditLogApiService.GetAuditLogsAsync(Filter);
        var optionsTask = _auditLogApiService.GetOptionsAsync();
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
        Add(query, "Filter.ModuleCode", Filter.ModuleCode);
        Add(query, "Filter.ActionType", Filter.ActionType);
        Add(query, "Filter.EntityName", Filter.EntityName);
        Add(query, "Filter.EntityId", Filter.EntityId);
        if (Filter.UserId.HasValue) query.Add($"Filter.UserId={Filter.UserId.Value}");
        if (Filter.FromDate.HasValue) query.Add($"Filter.FromDate={Filter.FromDate:yyyy-MM-dd}");
        if (Filter.ToDate.HasValue) query.Add($"Filter.ToDate={Filter.ToDate:yyyy-MM-dd}");
        return $"/Admin/AuditLogs?{string.Join('&', query)}";
    }

    public string FormatLocalTime(DateTime utc) =>
        VietnamTime.Format(utc);

    private static void Add(ICollection<string> query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            query.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
    }
}
