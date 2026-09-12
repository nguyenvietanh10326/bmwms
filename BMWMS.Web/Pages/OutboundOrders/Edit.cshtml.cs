using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.OutboundOrders;

[Authorize(Roles = "SALES_STAFF,PURCHASING_STAFF")]
public class EditModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    public EditModel(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    [BindProperty] public EditOutboundInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(long id)
    {
        var client = _httpClientFactory.CreateClient("ApiClient");
        var response = await client.GetAsync($"api/OutboundOrders/{id}");
        if (!response.IsSuccessStatusCode) return RedirectToPage("./Index");
        var order = await response.Content.ReadFromJsonAsync<OutboundOrderDetailDto>();
        if (order == null || order.Status != "DRAFT") return RedirectToPage("./Details", new { id });
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || order.CreatedByUserId != userId)
            return Forbid();
        if ((order.SourceType == "SALES_ORDER" && !User.IsInRole("SALES_STAFF")) ||
            (order.SourceType == "PURCHASE_RETURN" && !User.IsInRole("PURCHASING_STAFF")))
            return Forbid();

        Input = new EditOutboundInput
        {
            OutboundOrderId = order.OutboundOrderId,
            OutboundOrderNumber = order.OutboundOrderNumber,
            SourceType = order.SourceType,
            SourceReference = order.SourceReference,
            PartnerName = order.PartnerName,
            ExpectedIssueDate = order.ExpectedIssueDate ?? DateTime.Today,
            Notes = order.Notes,
            Items = order.Items.Select(item => new EditOutboundItemInput
            {
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                UnitName = item.UnitOfMeasure,
                QuantityScale = item.QuantityScale,
                RequestedQuantity = item.RequestedQuantity,
                MaximumQuantity = item.RequestedQuantity,
                Notes = item.Notes
            }).ToList()
        };
        await EnrichMaximumsAsync(client, order);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(long id)
    {
        if (id != Input.OutboundOrderId) return BadRequest();
        if (Input.Items.Count == 0 || Input.Items.Any(item => item.RequestedQuantity <= 0))
            ModelState.AddModelError(string.Empty, "Mọi mặt hàng trong phiếu phải có số lượng lớn hơn 0.");
        if (Input.SourceType == "PURCHASE_RETURN" && (Input.Notes?.Trim().Length ?? 0) < 10)
            ModelState.AddModelError(nameof(Input.Notes), "Phiếu trả nhà cung cấp phải ghi rõ lý do (ít nhất 10 ký tự).");
        if (!ModelState.IsValid) return Page();

        var payload = new
        {
            expectedIssueDate = Input.ExpectedIssueDate.ToString("yyyy-MM-dd"),
            notes = Input.Notes,
            items = Input.Items.Select(item => new
            {
                productId = item.ProductId,
                requestedQuantity = item.RequestedQuantity,
                notes = item.Notes
            })
        };
        var response = await _httpClientFactory.CreateClient("ApiClient")
            .PutAsJsonAsync($"api/OutboundOrders/{id}", payload);
        ApiMessageDto? result = null;
        try { result = await response.Content.ReadFromJsonAsync<ApiMessageDto>(); } catch { }
        if (response.IsSuccessStatusCode)
        {
            TempData["SuccessMessage"] = result?.Message ?? "Đã cập nhật phiếu xuất Nháp.";
            return RedirectToPage("./Details", new { id });
        }
        ModelState.AddModelError(string.Empty, result?.Message ?? "Không thể cập nhật phiếu xuất.");
        return Page();
    }

    private async Task EnrichMaximumsAsync(HttpClient client, OutboundOrderDetailDto order)
    {
        if (order.SourceType == "SALES_ORDER" && order.SalesOrderId.HasValue)
        {
            var source = await client.GetFromJsonAsync<SalesOrderDetailApiResponse>($"api/OutboundOrders/sales-order/{order.SalesOrderId.Value}");
            foreach (var item in Input.Items)
            {
                var sourceItem = source?.Items.FirstOrDefault(value => value.ProductId == item.ProductId);
                item.ReferenceQuantity = sourceItem?.Quantity ?? item.RequestedQuantity;
                item.MaximumQuantity = item.RequestedQuantity + (sourceItem?.ReservedQuantity ?? 0);
            }
        }
        else if (order.PurchaseOrderId.HasValue)
        {
            var source = await client.GetFromJsonAsync<PurchaseOrderDetailApiResponse>($"api/OutboundOrders/purchase-order/{order.PurchaseOrderId.Value}/return");
            foreach (var item in Input.Items)
            {
                var sourceItem = source?.Items.FirstOrDefault(value => value.ProductId == item.ProductId);
                item.ReferenceQuantity = sourceItem?.ReceivedQuantity ?? item.RequestedQuantity;
                item.MaximumQuantity = item.RequestedQuantity + (sourceItem?.RemainingQuantity ?? 0);
            }
        }
    }
}

public class EditOutboundInput
{
    public long OutboundOrderId { get; set; }
    public string OutboundOrderNumber { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public DateTime ExpectedIssueDate { get; set; }
    public string? Notes { get; set; }
    public List<EditOutboundItemInput> Items { get; set; } = new();
}

public class EditOutboundItemInput
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public byte QuantityScale { get; set; }
    public decimal ReferenceQuantity { get; set; }
    public decimal MaximumQuantity { get; set; }
    public decimal RequestedQuantity { get; set; }
    public string? Notes { get; set; }
}
