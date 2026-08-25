using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Inbound;

[Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,PURCHASING_STAFF")]
public class DetailsModel : PageModel
{
    private readonly InboundApiService _inboundApiService;
    public DetailsModel(InboundApiService inboundApiService) => _inboundApiService = inboundApiService;
    public InboundOrderDetailDto Order { get; set; } = default!;
    [BindProperty] public ReceiveInboundItemDto ReceiveDto { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(long id)
    {
        Order = await _inboundApiService.GetInboundOrderByIdAsync(id) ?? throw new InvalidOperationException("Không tìm thấy phiếu nhập kho.");
        if (User.IsInRole("WAREHOUSE_STAFF") &&
            (!long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId) ||
             Order.AssignedToUserId != userId))
            return Forbid();
        return Page();
    }

    public async Task<IActionResult> OnPostConfirmAsync(long id)
    {
        if (!User.IsInRole("SYSTEM_ADMIN") && !User.IsInRole("WAREHOUSE_MANAGER") && !User.IsInRole("PURCHASING_STAFF")) return Forbid();
        try { await _inboundApiService.ConfirmInboundOrderAsync(id); TempData["SuccessMessage"] = "Phiếu nhập kho đã được xác nhận."; }
        catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(long id, string reason)
    {
        if (!User.IsInRole("SYSTEM_ADMIN") && !User.IsInRole("WAREHOUSE_MANAGER") && !User.IsInRole("PURCHASING_STAFF")) return Forbid();
        try
        {
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Vui lòng nhập lý do hủy phiếu.");
            await _inboundApiService.CancelInboundOrderAsync(id, reason);
            TempData["SuccessMessage"] = "Phiếu nhập kho đã được hủy.";
        }
        catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReceiveAsync(long id)
    {
        if (!User.IsInRole("WAREHOUSE_STAFF")) return Forbid();
        try
        {
            await _inboundApiService.ReceiveItemAsync(id, ReceiveDto);
            TempData["SuccessMessage"] = "Đã kiểm đếm và lưu lần nhận hàng; tồn kho chưa thay đổi cho đến khi putaway.";
        }
        catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCompleteReceiptAsync(long id, List<long>? confirmedShortageItemIds, string? receiptNotes)
    {
        if (!User.IsInRole("WAREHOUSE_STAFF")) return Forbid();
        try
        {
            await _inboundApiService.CompleteReceiptAsync(id, confirmedShortageItemIds ?? new List<long>(), receiptNotes);
            TempData["SuccessMessage"] = "Đã hoàn tất kiểm nhận. Hãy xếp toàn bộ hàng đạt vào các vị trí kho chi tiết.";
        }
        catch (Exception ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToPage(new { id });
    }
}
