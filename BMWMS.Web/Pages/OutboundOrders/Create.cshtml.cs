using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace BMWMS.Web.Pages.OutboundOrders;

[Authorize(Roles = "SALES_STAFF,PURCHASING_STAFF")]
public class CreateModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    public CreateModel(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    [BindProperty] public OutboundOrderVM Input { get; set; } = new();
    public List<SelectListItem> SalesOrderOptions { get; set; } = new();
    public List<SelectListItem> PurchaseOrderOptions { get; set; } = new();
    public List<SelectListItem> AssigneeOptions { get; set; } = new();

    public async Task OnGetAsync(long? selectedSalesOrderId, long? selectedPurchaseOrderId)
    {
        Input.ExpectedIssueDate = DateTime.Today;
        var isPurchaseReturn = User.IsInRole("PURCHASING_STAFF");
        Input.SourceType = isPurchaseReturn ? "PURCHASE_RETURN" : "SALES_ORDER";
        Input.SalesOrderId = isPurchaseReturn ? null : selectedSalesOrderId;
        Input.PurchaseOrderId = isPurchaseReturn ? selectedPurchaseOrderId : null;
        await LoadDropdownsAsync();
        if (Input.SalesOrderId.HasValue) await LoadSalesOrderDetailAsync(Input.SalesOrderId.Value);
        if (Input.PurchaseOrderId.HasValue) await LoadPurchaseOrderDetailAsync(Input.PurchaseOrderId.Value);
    }

    public async Task<IActionResult> OnGetSalesOrderDetailAsync(long id)
        => User.IsInRole("SALES_STAFF")
            ? await ProxyJsonAsync($"api/OutboundOrders/sales-order/{id}")
            : Forbid();

    public async Task<IActionResult> OnGetPurchaseOrderDetailAsync(long id)
        => User.IsInRole("PURCHASING_STAFF")
            ? await ProxyJsonAsync($"api/OutboundOrders/purchase-order/{id}/return")
            : Forbid();

    public async Task<IActionResult> OnPostAsync()
    {
        var requiredSourceType = User.IsInRole("PURCHASING_STAFF") ? "PURCHASE_RETURN" : "SALES_ORDER";
        Input.SourceType = requiredSourceType;
        if (Input.SourceType == "SALES_ORDER" && !Input.SalesOrderId.HasValue)
            ModelState.AddModelError(string.Empty, "Vui lòng chọn đơn bán hàng (SO).");
        if (Input.SourceType == "PURCHASE_RETURN" && !Input.PurchaseOrderId.HasValue)
            ModelState.AddModelError(string.Empty, "Vui lòng chọn đơn mua hàng (PO) cần trả nhà cung cấp.");
        if (Input.Items == null || Input.Items.Count == 0)
            ModelState.AddModelError(string.Empty, "Đơn tham chiếu không còn mặt hàng có thể xuất.");
        if (!Input.AssignedToUserId.HasValue)
            ModelState.AddModelError(nameof(Input.AssignedToUserId), "Vui lòng chọn nhân viên kho phụ trách trước khi gửi phiếu.");
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        var payload = new CreateOutboundOrderRequest
        {
            SourceType = Input.SourceType,
            SalesOrderId = Input.SourceType == "SALES_ORDER" ? Input.SalesOrderId : null,
            PurchaseOrderId = Input.SourceType == "PURCHASE_RETURN" ? Input.PurchaseOrderId : null,
            ExpectedIssueDate = Input.ExpectedIssueDate.ToString("yyyy-MM-dd"),
            AssignedToUserId = Input.AssignedToUserId,
            Notes = Input.Notes,
            IsSubmit = true,
            Items = (Input.Items ?? new List<OutboundOrderItemVM>()).Select(i => new OutboundOrderItemRequest
            {
                ProductId = i.ProductId,
                RequestedQuantity = i.RequestedQuantity,
                Notes = i.Notes
            }).ToList()
        };

        try
        {
            var response = await _httpClientFactory.CreateClient("ApiClient").PostAsJsonAsync("api/OutboundOrders", payload);
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Đã tạo phiếu xuất và giao tác nghiệp lấy, xuất hàng cho nhân viên kho.";
                return RedirectToPage("./Index");
            }
            var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
            ModelState.AddModelError(string.Empty, error?.Message ?? error?.Detail ?? "Không thể tạo phiếu xuất kho.");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Không thể kết nối API: {ex.Message}");
        }
        await LoadDropdownsAsync();
        return Page();
    }

    private async Task<IActionResult> ProxyJsonAsync(string url)
    {
        try
        {
            var response = await _httpClientFactory.CreateClient("ApiClient").GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();
            return new ContentResult { StatusCode = (int)response.StatusCode, ContentType = "application/json", Content = json };
        }
        catch (Exception ex)
        {
            return new JsonResult(new { message = $"Không thể kết nối API: {ex.Message}" }) { StatusCode = 500 };
        }
    }

    private async Task LoadDropdownsAsync()
    {
        var client = _httpClientFactory.CreateClient("ApiClient");
        if (User.IsInRole("SALES_STAFF"))
        {
            try
            {
                var sales = await client.GetFromJsonAsync<List<SalesOrderOptionDto>>("api/OutboundOrders/sales-orders") ?? new();
                SalesOrderOptions = sales.Select(x => new SelectListItem(x.SalesOrderNumber, x.SalesOrderId.ToString())).ToList();
            }
            catch { SalesOrderOptions = new(); }
        }

        if (User.IsInRole("PURCHASING_STAFF"))
        {
            try
            {
                var purchases = await client.GetFromJsonAsync<List<PurchaseOrderOptionDto>>("api/OutboundOrders/purchase-orders/returnable") ?? new();
                PurchaseOrderOptions = purchases.Select(x => new SelectListItem($"{x.PurchaseOrderNumber} — {x.SupplierName}", x.PurchaseOrderId.ToString())).ToList();
            }
            catch { PurchaseOrderOptions = new(); }
        }

        try
        {
            var users = await client.GetFromJsonAsync<List<UserOptionDto>>("api/OutboundOrders/staff") ?? new();
            AssigneeOptions = users.Select(x => new SelectListItem(x.FullName ?? x.Username, x.UserId.ToString())).ToList();
        }
        catch { AssigneeOptions = new(); }
        SalesOrderOptions.Insert(0, new SelectListItem(
            SalesOrderOptions.Count == 0 ? "-- Không có SO đã xác nhận có thể xuất --" : "-- Chọn SO đã xác nhận --", ""));
        PurchaseOrderOptions.Insert(0, new SelectListItem(
            PurchaseOrderOptions.Count == 0 ? "-- Không có PO đã putaway còn hàng để trả --" : "-- Chọn PO đã nhập kho --", ""));
        AssigneeOptions.Insert(0, new SelectListItem(
            AssigneeOptions.Count == 0 ? "-- Không có nhân viên kho đang rảnh --" : "-- Chọn nhân viên kho đang rảnh --", ""));
    }

    private async Task LoadSalesOrderDetailAsync(long id)
    {
        var detail = await _httpClientFactory.CreateClient("ApiClient")
            .GetFromJsonAsync<SalesOrderDetailApiResponse>($"api/OutboundOrders/sales-order/{id}");
        if (detail == null) return;
        Input.PartnerName = detail.CustomerName;
        Input.SourceReference = detail.SalesOrderNumber;
        Input.Items = detail.Items.Select(x => new OutboundOrderItemVM
        {
            ProductId = x.ProductId, ProductCode = x.ProductCode, ProductName = x.ProductName,
            UnitName = x.UnitName, ReferenceQuantity = x.Quantity,
            AvailableQuantity = x.ReservedQuantity, RequestedQuantity = x.ReservedQuantity,
            QuantityScale = x.QuantityScale, TrackLot = x.TrackLot
        }).ToList();
    }

    private async Task LoadPurchaseOrderDetailAsync(long id)
    {
        var detail = await _httpClientFactory.CreateClient("ApiClient")
            .GetFromJsonAsync<PurchaseOrderDetailApiResponse>($"api/OutboundOrders/purchase-order/{id}/return");
        if (detail == null) return;
        Input.PartnerName = detail.SupplierName;
        Input.SourceReference = detail.PurchaseOrderNumber;
        Input.Items = detail.Items.Select(x => new OutboundOrderItemVM
        {
            ProductId = x.ProductId, ProductCode = x.ProductCode, ProductName = x.ProductName,
            UnitName = x.UnitName, ReferenceQuantity = x.ReceivedQuantity,
            AvailableQuantity = x.RemainingQuantity, RequestedQuantity = x.RemainingQuantity,
            QuantityScale = x.QuantityScale, TrackLot = x.TrackLot
        }).ToList();
    }
}

public class OutboundOrderVM
{
    [Required] public string SourceType { get; set; } = "SALES_ORDER";
    public long? SalesOrderId { get; set; }
    public long? PurchaseOrderId { get; set; }
    public string? SourceReference { get; set; }
    public string? PartnerName { get; set; }
    [Required] public DateTime ExpectedIssueDate { get; set; } = DateTime.Today;
    public long? AssignedToUserId { get; set; }
    public string? Notes { get; set; }
    public List<OutboundOrderItemVM> Items { get; set; } = new();
}
public class OutboundOrderItemVM
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public byte QuantityScale { get; set; }
    public bool TrackLot { get; set; }
    public decimal ReferenceQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal RequestedQuantity { get; set; }
    public string? Notes { get; set; }
}
public class CreateOutboundOrderRequest
{
    public string SourceType { get; set; } = string.Empty;
    public long? SalesOrderId { get; set; }
    public long? PurchaseOrderId { get; set; }
    public string ExpectedIssueDate { get; set; } = string.Empty;
    public long? AssignedToUserId { get; set; }
    public string? Notes { get; set; }
    public bool IsSubmit { get; set; }
    public List<OutboundOrderItemRequest> Items { get; set; } = new();
}
public class OutboundOrderItemRequest { public long ProductId { get; set; } public decimal RequestedQuantity { get; set; } public string? Notes { get; set; } }
public class SalesOrderOptionDto { public long SalesOrderId { get; set; } public string SalesOrderNumber { get; set; } = string.Empty; }
public class PurchaseOrderOptionDto { public long PurchaseOrderId { get; set; } public string PurchaseOrderNumber { get; set; } = string.Empty; public string SupplierName { get; set; } = string.Empty; }
public class UserOptionDto { public long UserId { get; set; } public string Username { get; set; } = string.Empty; public string? FullName { get; set; } }
public class SalesOrderDetailApiResponse { public string SalesOrderNumber { get; set; } = string.Empty; public string CustomerName { get; set; } = string.Empty; public long? WarehouseId { get; set; } public List<SalesOrderLineApiResponse> Items { get; set; } = new(); }
public class SalesOrderLineApiResponse { public long ProductId { get; set; } public string ProductCode { get; set; } = string.Empty; public string ProductName { get; set; } = string.Empty; public string UnitName { get; set; } = string.Empty; public byte QuantityScale { get; set; } public bool TrackLot { get; set; } public decimal Quantity { get; set; } public decimal ReservedQuantity { get; set; } }
public class PurchaseOrderDetailApiResponse { public string PurchaseOrderNumber { get; set; } = string.Empty; public string SupplierName { get; set; } = string.Empty; public List<PurchaseReturnLineApiResponse> Items { get; set; } = new(); }
public class PurchaseReturnLineApiResponse { public long ProductId { get; set; } public string ProductCode { get; set; } = string.Empty; public string ProductName { get; set; } = string.Empty; public string UnitName { get; set; } = string.Empty; public byte QuantityScale { get; set; } public bool TrackLot { get; set; } public decimal ReceivedQuantity { get; set; } public decimal RemainingQuantity { get; set; } }
public class ApiErrorResponse { public string? Message { get; set; } public string? Detail { get; set; } }
