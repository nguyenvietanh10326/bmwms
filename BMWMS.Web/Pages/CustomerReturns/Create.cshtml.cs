using BMWMS.Web.Models;
using BMWMS.Web.Models.Inventory;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.CustomerReturns;

[Authorize(Roles = "SALES_STAFF,SYSTEM_ADMIN")]
public class CreateModel(IHttpClientFactory factory) : PageModel
{
    [BindProperty(SupportsGet = true)] public long? SalesOrderId { get; set; }
    [BindProperty(SupportsGet = true)] public long? Id { get; set; }
    [BindProperty] public string? RowVersion { get; set; }
    [BindProperty] public string Reason { get; set; } = "";
    [BindProperty] public List<ReturnInput> Lines { get; set; } = new();
    public List<CustomerReturnSalesOrderModel> SalesOrders { get; set; } = new();
    public List<CustomerReturnSourceModel> Sources { get; set; } = new();
    private async Task LoadAsync()
    {
        var client = factory.CreateClient("ApiClient");
        SalesOrders = await client.GetFromJsonAsync<List<CustomerReturnSalesOrderModel>>("api/customer-returns/sales-orders") ?? new();
        if (SalesOrderId.HasValue) Sources = await client.GetFromJsonAsync<List<CustomerReturnSourceModel>>(
            $"api/customer-returns/sales-orders/{SalesOrderId}/sources" + (Id.HasValue ? $"?editingRequestId={Id}" : "")) ?? new();
    }
    public async Task<IActionResult> OnGetAsync()
    {
        try {
            CustomerReturnModel? existing = null;
            if (Id.HasValue)
            {
                existing = await factory.CreateClient("ApiClient").GetFromJsonAsync<CustomerReturnModel>($"api/customer-returns/{Id}");
                if (existing == null) return NotFound();
                if (!existing.CanEdit || (!User.IsInRole("SYSTEM_ADMIN") &&
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value != existing.CreatedByUserId.ToString())) return Forbid();
                SalesOrderId = existing.SalesOrderId; Reason = existing.Reason; RowVersion = existing.RowVersion;
            }
            await LoadAsync();
            Lines = Sources.Where(s => s.AvailableToReturn > 0).Select(s => new ReturnInput { ProductId = s.ProductId,
                Quantity = existing?.Items.FirstOrDefault(i => i.ProductId == s.ProductId)?.RequestedQuantity ?? 0 }).ToList();
        }
        catch { ModelState.AddModelError("", "Không tải được SO/nguồn thực giao. Vui lòng mở lại phiếu."); }
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            if (!SalesOrderId.HasValue || SalesOrderId <= 0) ModelState.AddModelError("", "Chọn SO tham chiếu.");
            if (Lines.Any(l => l.Quantity < 0)) ModelState.AddModelError("", "Số lượng trả không được âm.");
            if (ModelState.IsValid)
            {
                var client = factory.CreateClient("ApiClient");
                var body = new { SalesOrderId, Reason, RowVersion, Items = Lines.Where(l => l.Quantity > 0).ToList() };
                var response = Id.HasValue ? await client.PutAsJsonAsync($"api/customer-returns/{Id}", body)
                    : await client.PostAsJsonAsync("api/customer-returns", body);
                if (response.IsSuccessStatusCode) return RedirectToPage("./Details", new { id = Id ?? await response.Content.ReadFromJsonAsync<long>() });
                ModelState.AddModelError("", await ApiErrorReader.ReadAsync(response));
            }
            await LoadAsync();
        }
        catch { ModelState.AddModelError("", "Kết quả lưu chưa xác định do lỗi kết nối. Kiểm tra danh sách trước khi gửi lại."); }
        return Page();
    }
    public class ReturnInput { public long ProductId { get; set; } public decimal Quantity { get; set; } }
}
