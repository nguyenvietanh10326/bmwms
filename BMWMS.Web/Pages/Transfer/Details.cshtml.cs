using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static BMWMS.Web.Services.TransferApiService;

namespace BMWMS.Web.Pages.Transfer;

public class DetailsModel : PageModel
{
    private readonly TransferApiService _transferService;

    public DetailsModel(TransferApiService transferService) => _transferService = transferService;

    public TransferOrderDetailViewDto? Order { get; set; }

    [BindProperty(SupportsGet = true)]
    public long Id { get; set; }

    [TempData] public string? SuccessMessage { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public bool IsManager { get; set; }
    public bool IsStaff { get; set; }

    public async Task<IActionResult> OnGetAsync(long id)
    {
        if (id <= 0)
            return RedirectToPage("/Transfer/Index");

        Id = id;
        Order = await _transferService.GetOrderByIdAsync(id);
        if (Order == null)
            return NotFound();

        CheckUserRole();
        return Page();
    }

    public async Task<IActionResult> OnPostApproveAsync(long id, string? notes)
    {
        var result = await _transferService.ApproveOrderAsync(id, notes);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Đã phê duyệt phiếu thành công."
            : result.Message;
        return RedirectToPage("/Transfer/Details", new { id });
    }

    public async Task<IActionResult> OnPostCancelAsync(long id, string? notes)
    {
        var result = await _transferService.CancelOrderAsync(id, notes);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Đã hủy phiếu thành công."
            : result.Message;
        return RedirectToPage("/Transfer/Details", new { id });
    }

    public async Task<IActionResult> OnPostConfirmAsync(long id)
    {
        var request = new ConfirmTransferDto
        {
            AcknowledgeCapacityWarning = true,
            CapacityWarningReason = "Xác nhận theo số lượng và vị trí đã được duyệt trên phiếu chuyển kho."
        };
        var result = await _transferService.ConfirmTransferAsync(id, request);
        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
            ? "Đã xác nhận và hoàn thành chuyển kho."
            : result.Message;
        return RedirectToPage("/Transfer/Details", new { id });
    }

    private void CheckUserRole()
    {
        var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? string.Empty;
        IsManager = roleCode is "SYSTEM_ADMIN" or "WAREHOUSE_MANAGER";
        IsStaff = roleCode == "WAREHOUSE_STAFF";
    }
}
