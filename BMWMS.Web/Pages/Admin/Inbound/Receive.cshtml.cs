using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BMWMS.Web.Models;
using BMWMS.Web.Services;
using System;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.Admin.Inbound;

public class ReceiveModel : PageModel
{
    private readonly InboundApiService _inboundApi;

    public ReceiveModel(InboundApiService inboundApi)
    {
        _inboundApi = inboundApi;
    }

    [BindProperty(SupportsGet = true)]
    public InboundOrderFilterModel Filter { get; set; } = new InboundOrderFilterModel();

    public InboundOrderPageModel Data { get; set; } = new();

    public InboundOrderDetailDto Order { get; set; } = new();

    [BindProperty]
    public ReceiveBatchInboundDto ReceiveDto { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(long? id)
    {
        // Force filter to ASSIGNED or RECEIVING if not set
        if (string.IsNullOrEmpty(Filter.Status))
        {
            Filter.Status = "ASSIGNED,IN_PROGRESS"; // Backend might need support for comma separated, if not we just fetch all and filter in memory or backend handles it.
        }

        if (User.IsInRole("WAREHOUSE_STAFF"))
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out var currentUserId))
            {
                Filter.AssignedToUserId = currentUserId;
            }
        }

        Data = await _inboundApi.GetInboundOrdersPageAsync(Filter);

        // Filter the data to only include ASSIGNED and IN_PROGRESS
        Data.Items = Data.Items.Where(x => x.Status == "ASSIGNED" || x.Status == "IN_PROGRESS").ToList();

        if (id.HasValue)
        {
            var order = await _inboundApi.GetInboundOrderByIdAsync(id.Value);
            if (order != null) 
            {
                var userIdClaimForCheck = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (User.IsInRole("WAREHOUSE_STAFF") && long.TryParse(userIdClaimForCheck, out var userId) && order.AssignedToUserId != userId)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền nhận hàng cho lệnh này vì nó không được phân công cho bạn.";
                    return RedirectToPage("Index");
                }
                Order = order;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostReceiveBatchAsync(long id)
    {
        try
        {
            await _inboundApi.ReceiveBatchAsync(id, ReceiveDto);
            
            // Fetch order again to find items needing putaway
            var order = await _inboundApi.GetInboundOrderByIdAsync(id);
            var itemNeedingPutaway = order?.Items.FirstOrDefault(i => i.Receipts.Any(r => r.ReceivedQuantity > r.PutawayQuantity));
            
            if (itemNeedingPutaway != null)
            {
                var receipt = itemNeedingPutaway.Receipts.First(r => r.ReceivedQuantity > r.PutawayQuantity);
                return RedirectToPage("Putaway", new { id = id, itemId = itemNeedingPutaway.InboundOrderItemId, lotId = receipt.ProductLotId });
            }

            return RedirectToPage("Receive");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return await OnGetAsync(id);
        }
    }
}
