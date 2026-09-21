using BMWMS.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.CustomerReturns;

[Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF,WAREHOUSE_STAFF")]
public class IndexModel(IHttpClientFactory factory) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Keyword { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public string? Sort { get; set; }
    public List<CustomerReturnModel> Requests { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var canPrioritizeApproval = User.IsInRole("WAREHOUSE_MANAGER") || User.IsInRole("SYSTEM_ADMIN");
        var allowedSorts = canPrioritizeApproval ? new[] { "newest", "oldest", "priority" } : new[] { "newest", "oldest" };
        Sort = allowedSorts.Contains(Sort, StringComparer.OrdinalIgnoreCase)
            ? Sort!.ToLowerInvariant()
            : canPrioritizeApproval ? "priority" : "newest";
        ModelState.Remove(nameof(Sort));

        try
        {
            Requests = await factory.CreateClient("ApiClient")
                .GetFromJsonAsync<List<CustomerReturnModel>>("api/customer-returns") ?? new();
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Không tải được danh sách khách trả hàng. Vui lòng kiểm tra kết nối máy chủ.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(Keyword))
        {
            var keyword = Keyword.Trim();
            Requests = Requests.Where(request =>
                request.RequestNumber.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (request.SalesOrderNumber?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                request.CustomerName.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(Status))
            Requests = Requests.Where(request => request.Status == Status).ToList();

        Requests = Sort switch
        {
            "oldest" => Requests.OrderBy(request => request.CreatedAt).ToList(),
            "priority" when canPrioritizeApproval => Requests
                .OrderBy(request => request.Status is "SUBMITTED" or "PENDING_REMAINDER_REVIEW" ? 0 : request.Status is "APPROVED" or "PARTIALLY_RECEIVED" ? 1 : 2)
                .ThenByDescending(request => request.CreatedAt).ToList(),
            _ => Requests.OrderByDescending(request => request.CreatedAt).ToList()
        };
    }

    public static string Label(string status) => status switch
    {
        "SUBMITTED" => "Chờ duyệt", "APPROVED" => "Đã duyệt",
        "PARTIALLY_RECEIVED" => "Được nhận tiếp", "PENDING_REMAINDER_REVIEW" => "Chờ duyệt phần còn lại", "COMPLETED" => "Hoàn tất",
        "REJECTED" => "Từ chối", "CANCELLED" => "Đã hủy", _ => status
    };

    public static string StatusCss(string status) => status switch
    {
        "SUBMITTED" => "bg-warning-subtle text-warning-emphasis",
        "APPROVED" => "bg-primary-subtle text-primary-emphasis",
        "PARTIALLY_RECEIVED" => "bg-info-subtle text-info-emphasis", "PENDING_REMAINDER_REVIEW" => "bg-warning-subtle text-warning-emphasis",
        "COMPLETED" => "bg-success-subtle text-success-emphasis",
        "REJECTED" or "CANCELLED" => "bg-danger-subtle text-danger-emphasis",
        _ => "bg-secondary-subtle text-secondary-emphasis"
    };
}
