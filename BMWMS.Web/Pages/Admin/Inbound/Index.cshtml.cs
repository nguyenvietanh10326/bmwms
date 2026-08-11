using System.Threading.Tasks;
using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Inbound;

public class IndexModel : PageModel
{
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
        if (User.IsInRole("WAREHOUSE_STAFF"))
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out var currentUserId))
            {
                Filter.AssignedToUserId = currentUserId;
            }
        }

        Data = await _inboundApiService.GetInboundOrdersPageAsync(Filter);
        return Page();
    }
}
