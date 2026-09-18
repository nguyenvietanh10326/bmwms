using System.Threading.Tasks;
using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Inbound;

[Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,PURCHASING_STAFF,SALES_STAFF,ACCOUNTANT,DIRECTOR")]
public class IndexModel : PageModel
{
    private bool CanReadAllOrders => User.IsInRole("SYSTEM_ADMIN") || User.IsInRole("WAREHOUSE_MANAGER") || User.IsInRole("ACCOUNTANT") || User.IsInRole("DIRECTOR");
    private readonly InboundApiService _inboundApiService;

    public IndexModel(InboundApiService inboundApiService)
    {
        _inboundApiService = inboundApiService;
    }

    [BindProperty(SupportsGet = true)]
    public InboundOrderFilterModel Filter { get; set; } = new InboundOrderFilterModel();

    public InboundOrderPageModel Data { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        if (!CanReadAllOrders && User.IsInRole("WAREHOUSE_STAFF"))
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out var currentUserId))
            {
                Filter.AssignedToUserId = currentUserId;
            }
        }
        else if (!CanReadAllOrders && User.IsInRole("SALES_STAFF"))
        {
            Filter.SourceType = "SALES_RETURN";
        }
        else if (!CanReadAllOrders && User.IsInRole("PURCHASING_STAFF"))
        {
            Filter.SourceType = "PURCHASE_ORDER";
        }

        try { Data = await _inboundApiService.GetInboundOrdersPageAsync(Filter); }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        { ModelState.AddModelError(string.Empty, ex is InvalidOperationException ? ex.Message : "Không tải được phiếu nhập. Kiểm tra kết nối và schema database."); }
        return Page();
    }
}
