using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.CustomerReturns;
[Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF,WAREHOUSE_STAFF,ACCOUNTANT,DIRECTOR")]
public class DetailsModel(IHttpClientFactory factory) : PageModel
{
    [BindProperty(SupportsGet = true)] public long Id { get; set; }
    public CustomerReturnModel? ReturnRequest { get; set; }
    public async Task<IActionResult> OnGetAsync()
    {
        if (Id <= 0) { ModelState.AddModelError("", "Thiếu mã yêu cầu trả hàng. Vui lòng mở lại phiếu từ danh sách."); return Page(); }
        try {
            using var response = await factory.CreateClient("ApiClient").GetAsync($"api/customer-returns/{Id}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                ModelState.AddModelError("", "Yêu cầu trả hàng không tồn tại. Vui lòng kiểm tra lại danh sách.");
            else if (!response.IsSuccessStatusCode)
                ModelState.AddModelError("", await ApiErrorReader.ReadAsync(response));
            else ReturnRequest = await response.Content.ReadFromJsonAsync<CustomerReturnModel>();
        }
        catch { ModelState.AddModelError("", "Không tải được yêu cầu trả hàng."); }
        return Page();
    }
    public async Task<IActionResult> OnPostDecisionAsync(string action, string? reason, string rowVersion)
    {
        if (!User.IsInRole("SYSTEM_ADMIN") && !User.IsInRole("WAREHOUSE_MANAGER") && !User.IsInRole("SALES_STAFF")) return Forbid();
        if (Id <= 0) { TempData["ErrorMessage"] = "Thiếu mã yêu cầu trả hàng. Vui lòng mở lại phiếu từ danh sách."; return RedirectToPage("./Index"); }
        try
        {
            var response = await factory.CreateClient("ApiClient").PostAsJsonAsync($"api/customer-returns/{Id}/decision", new { action, reason, rowVersion });
            TempData[response.IsSuccessStatusCode ? "SuccessMessage" : "ErrorMessage"] = response.IsSuccessStatusCode ? "Đã cập nhật yêu cầu trả hàng." : await ApiErrorReader.ReadAsync(response);
        }
        catch { TempData["ErrorMessage"] = "Kết quả chưa xác định. Tải lại và kiểm tra trạng thái trước khi thao tác tiếp."; }
        return RedirectToPage(new { Id });
    }
    public async Task<IActionResult> OnPostReceiptAsync(DateOnly receiptDate)
    {
        if (!User.IsInRole("WAREHOUSE_STAFF")) return Forbid();
        if (Id <= 0) { TempData["ErrorMessage"] = "Thiếu mã yêu cầu trả hàng. Vui lòng mở lại phiếu từ danh sách."; return RedirectToPage("./Index"); }
        try
        {
            var response = await factory.CreateClient("ApiClient").PostAsync($"api/customer-returns/{Id}/receipts?receiptDate={receiptDate:yyyy-MM-dd}", null);
            if (response.IsSuccessStatusCode) return RedirectToPage("/Admin/Inbound/Details", new { id = await response.Content.ReadFromJsonAsync<long>() });
            TempData["ErrorMessage"] = await ApiErrorReader.ReadAsync(response);
        }
        catch { TempData["ErrorMessage"] = "Kết quả tạo phiếu chưa xác định. Kiểm tra các đợt nhận trước khi thử lại."; }
        return RedirectToPage(new { Id });
    }
}
