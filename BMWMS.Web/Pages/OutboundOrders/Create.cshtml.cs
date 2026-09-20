using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using BMWMS.Web.Services;

namespace BMWMS.Web.Pages.OutboundOrders;

[Authorize(Roles = "WAREHOUSE_STAFF,SYSTEM_ADMIN")]
public class CreateModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    public CreateModel(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    [BindProperty] public OutboundOrderVM Input { get; set; } = new();
    public List<SelectListItem> SalesOrderOptions { get; set; } = new();
    public List<SelectListItem> PurchaseOrderOptions { get; set; } = new();
    public List<SelectListItem> AssigneeOptions { get; set; } = new();

    public async Task OnGetAsync(long? selectedSalesOrderId, long? selectedPurchaseOrderId, long? salesOrderId = null, long? purchaseOrderId = null, string? sourceType = null)
    {
        // Chấp nhận cả tên query cũ để các liên kết từ màn SO/PO không bị gãy.
        selectedSalesOrderId ??= salesOrderId;
        selectedPurchaseOrderId ??= purchaseOrderId;
        Input.ExpectedIssueDate = DateTime.Today;
        Input.SourceType = selectedPurchaseOrderId.HasValue || string.Equals(sourceType, "PURCHASE_RETURN", StringComparison.OrdinalIgnoreCase)
            ? "PURCHASE_RETURN" : "SALES_ORDER";
        Input.SalesOrderId = Input.SourceType == "SALES_ORDER" ? selectedSalesOrderId : null;
        Input.PurchaseOrderId = Input.SourceType == "PURCHASE_RETURN" ? selectedPurchaseOrderId : null;
        await LoadDropdownsAsync();
        if (Input.SalesOrderId.HasValue) await LoadSalesOrderDetailAsync(Input.SalesOrderId.Value);
        if (Input.PurchaseOrderId.HasValue) await LoadPurchaseOrderDetailAsync(Input.PurchaseOrderId.Value);
    }

    public async Task<IActionResult> OnGetSalesOrderDetailAsync(long id)
        => User.IsInRole("WAREHOUSE_STAFF") || User.IsInRole("SYSTEM_ADMIN")
            ? await ProxyJsonAsync($"api/OutboundOrders/sales-order/{id}")
            : Forbid();

    public async Task<IActionResult> OnGetPurchaseOrderDetailAsync(long id)
        => User.IsInRole("WAREHOUSE_STAFF") || User.IsInRole("SYSTEM_ADMIN")
            ? await ProxyJsonAsync($"api/OutboundOrders/purchase-order/{id}/return")
            : Forbid();

    public async Task<IActionResult> OnPostAsync()
    {
        Input.SourceType = Input.SourceType?.Trim().ToUpperInvariant() ?? string.Empty;
        if (Input.SourceType is not ("SALES_ORDER" or "PURCHASE_RETURN"))
            ModelState.AddModelError("Input.SourceType", "Vui lòng chọn nghiệp vụ xuất kho.");
        if (Input.SourceType == "SALES_ORDER" && !Input.SalesOrderId.HasValue)
            ModelState.AddModelError(string.Empty, "Vui lòng chọn đơn bán hàng (SO).");
        if (Input.SourceType == "PURCHASE_RETURN" && !Input.PurchaseOrderId.HasValue)
            ModelState.AddModelError(string.Empty, "Vui lòng chọn đơn mua hàng (PO) cần trả nhà cung cấp.");
        if (User.IsInRole("SYSTEM_ADMIN") && !Input.AssignedToUserId.HasValue)
            ModelState.AddModelError("Input.AssignedToUserId", "Vui lòng chọn Nhân viên kho thực hiện phiếu xuất.");
        if (Input.Items == null || Input.Items.Count == 0)
            ModelState.AddModelError(string.Empty, "Đơn tham chiếu không còn mặt hàng có thể xuất.");
        var selectedItems = (Input.Items ?? new List<OutboundOrderItemVM>())
            .Where(item => item.ProductId > 0 && item.AvailableQuantity > 0)
            .ToList();
        if (selectedItems.Count == 0)
            ModelState.AddModelError(string.Empty, "Đơn tham chiếu không còn mặt hàng có thể xuất.");
        if (Input.SourceType == "PURCHASE_RETURN" && (Input.Notes?.Trim().Length ?? 0) < 10)
            ModelState.AddModelError("Input.Notes", "Phiếu trả nhà cung cấp phải ghi rõ lý do (ít nhất 10 ký tự).");
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
            AssignedToUserId = User.IsInRole("SYSTEM_ADMIN") ? Input.AssignedToUserId : null,
            Notes = Input.Notes,
            IsSubmit = true,
            Items = selectedItems.Select(i => new OutboundOrderItemRequest
            {
                ProductId = i.ProductId,
                RequestedQuantity = i.AvailableQuantity,
                Notes = i.Notes
            }).ToList()
        };

        try
        {
            var response = await _httpClientFactory.CreateClient("ApiClient").PostAsJsonAsync("api/OutboundOrders", payload);
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = Input.SourceType == "SALES_ORDER"
                    ? "Đã tạo và tự nhận đợt xuất bán. Phiếu sẵn sàng để kiểm tra hàng vật lý."
                    : "Đã tạo và tự nhận đợt trả nhà cung cấp. Phiếu sẵn sàng để kiểm tra hàng vật lý.";
                return RedirectToPage("./Index");
            }
            ModelState.AddModelError(string.Empty, await ApiErrorReader.ReadAsync(response));
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Không thể kết nối đến hệ thống. Vui lòng thử lại.");
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
        catch (Exception)
        {
            return new JsonResult(new { message = "Không thể kết nối để tải đơn tham chiếu." }) { StatusCode = 500 };
        }
    }

    private async Task LoadDropdownsAsync()
    {
        var client = _httpClientFactory.CreateClient("ApiClient");
        if (User.IsInRole("WAREHOUSE_STAFF") || User.IsInRole("SYSTEM_ADMIN"))
        {
            try
            {
                var sales = await client.GetFromJsonAsync<List<SalesOrderOptionDto>>("api/OutboundOrders/sales-orders") ?? new();
                SalesOrderOptions = sales.Select(x => new SelectListItem(x.SalesOrderNumber, x.SalesOrderId.ToString())).ToList();
            }
            catch { SalesOrderOptions = new(); }
        }

        if (User.IsInRole("WAREHOUSE_STAFF") || User.IsInRole("SYSTEM_ADMIN"))
        {
            try
            {
                var purchases = await client.GetFromJsonAsync<List<PurchaseOrderOptionDto>>("api/OutboundOrders/purchase-orders/returnable") ?? new();
                PurchaseOrderOptions = purchases.Select(x => new SelectListItem($"{x.PurchaseOrderNumber} — {x.SupplierName}", x.PurchaseOrderId.ToString())).ToList();
            }
            catch { PurchaseOrderOptions = new(); }

            try
            {
                var staff = await client.GetFromJsonAsync<List<UserOptionDto>>("api/OutboundOrders/staff") ?? new();
                AssigneeOptions = staff.Select(x => new SelectListItem(x.FullName ?? x.Username, x.UserId.ToString())).ToList();
            }
            catch { AssigneeOptions = new(); }
        }

        SalesOrderOptions.Insert(0, new SelectListItem(
            SalesOrderOptions.Count == 0 ? "-- Không có SO đã xác nhận có thể xuất --" : "-- Chọn SO đã xác nhận --", ""));
        PurchaseOrderOptions.Insert(0, new SelectListItem(
            PurchaseOrderOptions.Count == 0 ? "-- Không có PO đã cất kho còn hàng để trả --" : "-- Chọn PO đã nhập kho --", ""));
        AssigneeOptions.Insert(0, new SelectListItem(
            AssigneeOptions.Count == 0 ? "-- Không có Nhân viên kho đang hoạt động --" : "-- Chọn Nhân viên kho --", ""));
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
            AvailableQuantity = x.ReservedQuantity,
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
            UnitName = x.UnitName, ReferenceQuantity = x.ReceivedQuantity, SourceLocations = x.SourceLocations,
            AvailableQuantity = x.RemainingQuantity,
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
    public List<ReturnLocationVM> SourceLocations { get; set; } = new();
    public long ProductId { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public string ProductCode { get; set; } = string.Empty;
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public string ProductName { get; set; } = string.Empty;
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public string UnitName { get; set; } = string.Empty;
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
public class OutboundOrderItemRequest { public long ProductId { get; set; } public decimal RequestedQuantity { get; set; } public string? Notes { get; set; } public List<ReturnLocationVM> RequestedLocations { get; set; } = new(); }
public class ReturnLocationVM
{
    public long StorageLocationId { get; set; }
    public long ProductLotId { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever] public string LocationPath { get; set; } = string.Empty;
    public DateOnly FirstReceivedDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal StoredQuantity { get; set; }
    public decimal HeldQuantity { get; set; }
    public decimal ReturnableQuantity { get; set; }
    public decimal Quantity { get; set; }
}
public class SalesOrderOptionDto { public long SalesOrderId { get; set; } public string SalesOrderNumber { get; set; } = string.Empty; }
public class PurchaseOrderOptionDto { public long PurchaseOrderId { get; set; } public string PurchaseOrderNumber { get; set; } = string.Empty; public string SupplierName { get; set; } = string.Empty; }
public class UserOptionDto { public long UserId { get; set; } public string Username { get; set; } = string.Empty; public string? FullName { get; set; } }
public class SalesOrderDetailApiResponse { public string SalesOrderNumber { get; set; } = string.Empty; public string CustomerName { get; set; } = string.Empty; public long? WarehouseId { get; set; } public List<SalesOrderLineApiResponse> Items { get; set; } = new(); }
public class SalesOrderLineApiResponse { public long ProductId { get; set; } public string ProductCode { get; set; } = string.Empty; public string ProductName { get; set; } = string.Empty; public string UnitName { get; set; } = string.Empty; public byte QuantityScale { get; set; } public bool TrackLot { get; set; } public decimal Quantity { get; set; } public decimal ReservedQuantity { get; set; } }
public class PurchaseOrderDetailApiResponse { public string PurchaseOrderNumber { get; set; } = string.Empty; public string SupplierName { get; set; } = string.Empty; public List<PurchaseReturnLineApiResponse> Items { get; set; } = new(); }
public class PurchaseReturnLineApiResponse { public List<ReturnLocationVM> SourceLocations { get; set; } = new(); public long ProductId { get; set; } public string ProductCode { get; set; } = string.Empty; public string ProductName { get; set; } = string.Empty; public string UnitName { get; set; } = string.Empty; public byte QuantityScale { get; set; } public bool TrackLot { get; set; } public decimal ReceivedQuantity { get; set; } public decimal RemainingQuantity { get; set; } }
