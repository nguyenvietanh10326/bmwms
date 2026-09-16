using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace BMWMS.Web.Pages.Admin.Inbound;

[Authorize(Roles = "WAREHOUSE_STAFF")]
public class ReturnReceiptModel(InboundApiService inbound, IHttpClientFactory factory) : PageModel
{
    public InboundOrderDetailDto Order { get; set; } = null!;
    [BindProperty] public DateOnly ReceiptDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [BindProperty] public string? ShortageReason { get; set; }
    [BindProperty] public List<ReturnReceiptLine> Lines { get; set; } = new();
    private async Task<bool> LoadAsync(long id)
    {
        Order = (await inbound.GetInboundOrderByIdAsync(id))!;
        return Order != null && Order.SourceType == "SALES_RETURN" && Order.Status == "RECEIVING" &&
            long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId) &&
            Order.AssignedToUserId == userId;
    }
    public async Task<IActionResult> OnGetAsync(long id)
    {
        if (!await LoadAsync(id)) return Forbid();
        Lines = Order.Items.Select(i => new ReturnReceiptLine { InboundOrderItemId = i.InboundOrderItemId }).ToList();
        return Page();
    }
    public async Task<IActionResult> OnPostAsync(long id)
    {
        if (!await LoadAsync(id)) return Forbid();
        if (ModelState.IsValid)
        {
            try {
                var response = await factory.CreateClient("ApiClient").PostAsJsonAsync($"api/inbounds/{id}/customer-return/receive",
                    new { ReceiptDate, ShortageReason, Items = Lines });
                if (response.IsSuccessStatusCode) {
                    TempData["SuccessMessage"] = "Đã chốt số thực nhận. Tiếp tục cất hàng vào vị trí.";
                    return RedirectToPage("./Putaway", new { id });
                }
                ModelState.AddModelError("", await ApiErrorReader.ReadAsync(response));
            }
            catch { ModelState.AddModelError("", "Không thể kết nối để lưu thực nhận."); }
        }
        return Page();
    }
}
public class ReturnReceiptLine {
    public long InboundOrderItemId { get; set; }
    [Required(ErrorMessage = "Nhập số thực nhận cho mỗi mặt hàng; nhập 0 nếu chưa nhận.")] public decimal? ActualQuantity { get; set; }
    public DateOnly? ManufactureDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}
