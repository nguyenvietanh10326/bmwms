using BMWMS.Web.Models.Inventory;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Orders;

[Authorize(Roles = "WAREHOUSE_MANAGER,SYSTEM_ADMIN")]
public class CancelApprovedModel(IHttpClientFactory factory) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Type { get; set; }
    [BindProperty(SupportsGet = true)] public long Id { get; set; }
    [BindProperty] public string Reason { get; set; } = "";
    public string OrderNumber { get; set; } = "";
    public string PartnerName { get; set; } = "";
    public string BackUrl => Type == "PO" ? "/PurchaseOrders" : "/sales-orders";
    public bool CanCancel { get; set; }

    private async Task LoadAsync()
    {
        var client = factory.CreateClient("ApiClient");
        if (Type == "PO")
        {
            var po = await client.GetFromJsonAsync<PurchaseOrderDetailDto>($"api/PurchaseOrders/{Id}");
            OrderNumber = po?.PurchaseOrderNumber ?? ""; PartnerName = po?.SupplierName ?? "";
            CanCancel = po?.CanCancel == true && po.Status is "APPROVED" or "CONFIRMED";
        }
        else
        {
            var response = await client.GetFromJsonAsync<SalesOrderReply>($"api/salesorders/{Id}");
            var so = response?.Data;
            OrderNumber = so?.SalesOrderNumber ?? ""; PartnerName = so?.CustomerName ?? "";
            CanCancel = so?.CanExternalCancel == true;
        }
        if (!CanCancel) ModelState.AddModelError("", "Đơn không còn đủ điều kiện hủy: phải đã duyệt và chưa có phiếu nhập/xuất.");
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id <= 0 || Type is not ("PO" or "SO")) return BadRequest();
        try { await LoadAsync(); }
        catch { ModelState.AddModelError("", "Không tải được đơn hàng. Vui lòng quay lại danh sách."); }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Id <= 0 || Type is not ("PO" or "SO")) return BadRequest();
        try
        {
            await LoadAsync();
            if ((Reason?.Trim().Length ?? 0) is < 10 or > 500) ModelState.AddModelError("", "Nhập căn cứ hủy từ NCC/khách hàng, từ 10 đến 500 ký tự.");
            if (ModelState.IsValid && CanCancel)
            {
                var path = Type == "PO" ? "api/PurchaseOrders" : "api/salesorders";
                var client = factory.CreateClient("ApiClient");
                using var response = Type == "PO"
                    ? await client.PostAsync($"{path}/{Id}/cancel?reason={Uri.EscapeDataString(Reason!.Trim())}", null)
                    : await client.PostAsJsonAsync($"{path}/{Id}/cancel", new { reason = Reason!.Trim() });
                if (response.IsSuccessStatusCode) { TempData["SuccessMessage"] = "Đã ghi nhận hủy theo yêu cầu bên ngoài."; return LocalRedirect(BackUrl); }
                ModelState.AddModelError("", await ApiErrorReader.ReadAsync(response));
            }
        }
        catch { ModelState.AddModelError("", "Kết quả chưa xác định do lỗi kết nối. Kiểm tra trạng thái đơn trước khi thử lại."); }
        return Page();
    }
    public class SalesOrderReply { public SalesOrderDetailDto? Data { get; set; } }
}
