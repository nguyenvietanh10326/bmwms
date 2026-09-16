using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMWMS.Web.Pages.Admin.Inbound;

[Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
public class AuthorizeReturnModel(InboundApiService inbound, IHttpClientFactory factory) : PageModel
{
    [BindProperty(SupportsGet = true)] public long? SalesOrderId { get; set; }
    [BindProperty] public long? StaffId { get; set; }
    [BindProperty] public DateOnly ReceiptDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [BindProperty] public string? Reason { get; set; }
    [BindProperty] public List<ReturnPlanLine> Lines { get; set; } = new();
    public List<SelectListItem> Orders { get; set; } = new();
    public List<SelectListItem> Staff { get; set; } = new();
    public PurchaseOrderForInboundDto? Source { get; set; }

    private async Task LoadAsync()
    {
        Orders = (await inbound.GetReturnableSalesOrdersAsync()).Select(o => new SelectListItem(o.Name, o.Id.ToString())).ToList();
        Staff = (await factory.CreateClient("ApiClient").GetFromJsonAsync<List<AvailableWarehouseStaffDto>>("api/inbounds/staff/available") ?? new())
            .Select(s => new SelectListItem(s.FullName, s.UserId.ToString())).ToList();
        if (SalesOrderId.HasValue) Source = await inbound.GetSalesOrderForInboundAsync(SalesOrderId.Value);
    }
    public async Task<IActionResult> OnGetAsync()
    {
        try { await LoadAsync(); }
        catch { ModelState.AddModelError("", "Không tải được đơn nguồn hoặc nhân viên kho."); }
        if (Source != null) Lines = Source.Items.Where(i => i.RemainingQuantity > 0)
            .Select(i => new ReturnPlanLine { ProductId = i.ProductId }).ToList();
        return Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!SalesOrderId.HasValue || !StaffId.HasValue || (Reason?.Trim().Length ?? 0) < 10)
            ModelState.AddModelError("", "Chọn SO, nhân viên kho và ghi căn cứ chấp thuận ít nhất 10 ký tự.");
        if (ModelState.IsValid)
        {
            var dto = new CreateInboundOrderDto { SourceType = "SALES_RETURN", SalesOrderId = SalesOrderId,
                AssignedToUserId = StaffId, ExpectedReceiptDate = ReceiptDate, Notes = Reason,
                Items = Lines.Select(l => new CreateInboundOrderItemDto { ProductId = l.ProductId, ExpectedQuantity = l.Quantity }).ToList() };
            try
            {
                var response = await factory.CreateClient("ApiClient").PostAsJsonAsync("api/inbounds/customer-return/authorize", dto);
                if (response.IsSuccessStatusCode) {
                    var id = await response.Content.ReadFromJsonAsync<long>();
                    TempData["SuccessMessage"] = "Đã duyệt và giao lệnh nhận hàng khách trả.";
                    return RedirectToPage("./Details", new { id });
                }
                ModelState.AddModelError("", await ApiErrorReader.ReadAsync(response));
            }
            catch { ModelState.AddModelError("", "Không thể kết nối để duyệt lệnh nhận trả."); }
        }
        try { await LoadAsync(); } catch { ModelState.AddModelError("", "Không tải được dữ liệu đơn nguồn."); }
        return Page();
    }
}
public class ReturnPlanLine { public long ProductId { get; set; } public decimal Quantity { get; set; } }
