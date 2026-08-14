using BMWMS.Web.Models.Inventory;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.PurchaseOrders;

public class DetailModel : PageModel
{
    private readonly PurchaseOrderApiService _apiService;

    public DetailModel(PurchaseOrderApiService apiService)
    {
        _apiService = apiService;
    }

    [BindProperty(SupportsGet = true)]
    public long Id { get; set; }

    public PurchaseOrderDetailDto? Order { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Order = await _apiService.GetPurchaseOrderByIdAsync(Id);
        if (Order == null)
        {
            return NotFound();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync()
    {
        var (isSuccess, message) = await _apiService.ConfirmPurchaseOrderAsync(Id);
        if (isSuccess)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostCancelAsync(string? reason)
    {
        var (isSuccess, message) = await _apiService.CancelPurchaseOrderAsync(Id, reason);
        if (isSuccess)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }
        return RedirectToPage(new { id = Id });
    }
}
