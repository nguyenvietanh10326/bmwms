using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Inbound;

[Authorize(Roles = "WAREHOUSE_STAFF")]
public class ReceiptModel : PageModel
{
    private readonly InboundApiService _inboundApiService;

    public ReceiptModel(InboundApiService inboundApiService) => _inboundApiService = inboundApiService;

    public InboundOrderDetailDto Order { get; set; } = default!;

    [BindProperty]
    public ReceiveBatchInboundDto Batch { get; set; } = new();

    [BindProperty]
    public CompleteInboundReceiptDto CompleteDto { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var result = await LoadOrderAsync(id);
        if (result != null) return result;

        Batch.Items = Order.Items.Select(item => new ReceiveInboundItemDto
        {
            InboundOrderItemId = item.InboundOrderItemId
        }).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveProgressAsync(long id)
    {
        var result = await LoadOrderAsync(id);
        if (result != null) return result;

        try
        {
            await _inboundApiService.ReceiveBatchAsync(id, Batch);
            TempData["SuccessMessage"] = "Đã lưu tiến độ kiểm nhận. Tồn tại bin chưa thay đổi.";
            return RedirectToPage(new { id });
        }
        catch (Exception exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostCompleteAsync(long id)
    {
        var result = await LoadOrderAsync(id);
        if (result != null) return result;

        try
        {
            await _inboundApiService.CompleteReceiptAsync(id, CompleteDto.Decisions, CompleteDto.Notes);
            TempData["SuccessMessage"] = "Đã hoàn tất kiểm nhận. Hàng đạt đang chờ xác nhận vị trí cất.";
            return RedirectToPage("./Details", new { id });
        }
        catch (Exception exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }
    }

    private async Task<IActionResult?> LoadOrderAsync(long id)
    {
        var order = await _inboundApiService.GetInboundOrderByIdAsync(id);
        if (order == null) return NotFound();
        Order = order;

        if (!long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId) ||
            Order.AssignedToUserId != userId)
            return Forbid();

        if (Order.Status is not ("READY" or "RECEIVING"))
        {
            TempData["ErrorMessage"] = "Phiếu không còn ở bước kiểm nhận.";
            return RedirectToPage("./Details", new { id });
        }

        return null;
    }
}
