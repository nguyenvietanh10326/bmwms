using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Inbound;

[Authorize(Roles = "WAREHOUSE_STAFF")]
public class ReceiveModel : PageModel
{
    private readonly InboundApiService _inboundApi;

    public ReceiveModel(InboundApiService inboundApi) => _inboundApi = inboundApi;

    [BindProperty(SupportsGet = true)]
    public InboundOrderFilterModel Filter { get; set; } = new();

    public InboundOrderPageModel Data { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(long? id)
    {
        if (id.HasValue)
            return RedirectToPage("./Details", new { id = id.Value });

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out var currentUserId))
            return Forbid();

        // Truy vấn theo chính nhân viên được giao. Không truyền chuỗi nhiều trạng thái
        // vì trạng thái lưu DB và trạng thái hiển thị không cùng tên.
        Filter.Status = null;
        Filter.AssignedToUserId = currentUserId;
        Filter.PageSize = 100;
        Data = await _inboundApi.GetInboundOrdersPageAsync(Filter);
        Data.Items = Data.Items
            .Where(x => x.Status is "READY" or "RECEIVING" or "RECEIVED")
            .OrderBy(x => x.ExpectedReceiptDate)
            .ThenBy(x => x.CreatedAt)
            .ToList();
        Data.TotalCount = Data.Items.Count;

        return Page();
    }
}
