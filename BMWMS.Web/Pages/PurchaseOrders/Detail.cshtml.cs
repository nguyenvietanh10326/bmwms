using BMWMS.Web.Models.Inventory;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.PurchaseOrders;

[Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF")]
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

    public async Task<IActionResult> OnPostSendToSupplierAsync()
    {
        if (!User.IsInRole("SYSTEM_ADMIN") && !User.IsInRole("PURCHASING_STAFF"))
            return Forbid();

        var (isSuccess, message) = await _apiService.SendPurchaseOrderToSupplierAsync(Id);
        if (isSuccess)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

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

    public async Task<IActionResult> OnPostClosePartialAsync(string reason)
    {
        if (!User.IsInRole("SYSTEM_ADMIN") && !User.IsInRole("WAREHOUSE_MANAGER"))
            return Forbid();
        var (isSuccess, message) = await _apiService.ClosePartiallyReceivedOrderAsync(Id, reason);
        TempData[isSuccess ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostContinuePartialAsync()
    {
        if (!User.IsInRole("SYSTEM_ADMIN") && !User.IsInRole("WAREHOUSE_MANAGER"))
            return Forbid();
        var (isSuccess, message) = await _apiService.ContinuePartiallyReceivedOrderAsync(Id);
        TempData[isSuccess ? "SuccessMessage" : "ErrorMessage"] = message;
        return RedirectToPage(new { id = Id });
    }

    public string GetPurchaseOrderStatusLabel(string? status) => (status ?? string.Empty).ToUpperInvariant() switch
    {
        "DRAFT" => "Nháp",
        "PENDING_CONFIRMATION" => "Chờ NCC phản hồi",
        "CONFIRMED" => "Đã xác nhận",
        "PENDING_RECEIPT_REVIEW" => "Chờ duyệt nhận tiếp",
        "PARTIALLY_RECEIVED" => "Đã nhận một phần",
        "RECEIVED" => "Đã nhận đủ",
        "CLOSED" => "Đã kết thúc nhận",
        "CANCELLED" => "Đã hủy",
        _ => "Không xác định"
    };

    public string GetInboundStatusLabel(string? status) => (status ?? string.Empty).ToUpperInvariant() switch
    {
        "DRAFT" => "Nháp",
        "READY" => "Sẵn sàng nhận",
        "RECEIVING" => "Đang nhận hàng",
        "RECEIVED" => "Đã nhận hàng",
        "PUTAWAY_COMPLETED" => "Đã xếp vị trí",
        "CANCELLED" => "Đã hủy",
        _ => "Không xác định"
    };

    public string GetStatusClass(string? status) => (status ?? string.Empty).ToUpperInvariant() switch
    {
        "CONFIRMED" or "READY" => "bg-primary-subtle text-primary",
        "PENDING_CONFIRMATION" or "PENDING_RECEIPT_REVIEW" or "PARTIALLY_RECEIVED" or "RECEIVING" => "bg-warning-subtle text-warning-emphasis",
        "RECEIVED" or "PUTAWAY_COMPLETED" or "CLOSED" => "bg-success-subtle text-success",
        "CANCELLED" => "bg-danger-subtle text-danger",
        _ => "bg-secondary-subtle text-secondary"
    };
}
