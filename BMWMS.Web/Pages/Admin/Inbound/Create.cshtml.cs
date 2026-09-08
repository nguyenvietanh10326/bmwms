using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

using Microsoft.AspNetCore.Authorization;

namespace BMWMS.Web.Pages.Admin.Inbound;

[Authorize(Roles = "WAREHOUSE_STAFF")]
public class CreateModel : PageModel
{
    private readonly InboundApiService _inboundApiService;

    public CreateModel(InboundApiService inboundApiService)
    {
        _inboundApiService = inboundApiService;
    }

    [BindProperty]
    public CreateInboundOrderDto InboundOrder { get; set; } = new();

    public List<PurchaseOrderInboundSourceDto> PurchaseOrderSources { get; set; } = new();
    public IEnumerable<PurchaseOrderInboundSourceDto> InitialPurchaseOrders =>
        PurchaseOrderSources.Where(source => !source.IsFollowUpReceipt);
    public IEnumerable<PurchaseOrderInboundSourceDto> FollowUpPurchaseOrders =>
        PurchaseOrderSources.Where(source => source.IsFollowUpReceipt);
    public SelectList SalesOrders { get; set; } = new(Array.Empty<object>());
    public async Task<IActionResult> OnGetAsync(long? purchaseOrderId, long? salesOrderId, long? parentInboundOrderId)
    {
        if (parentInboundOrderId.HasValue)
        {
            TempData["ErrorMessage"] = "Chức năng phiếu nhập bổ sung đã được loại bỏ. Hãy tạo phiếu nhập PO thông thường cho phần PO còn thiếu.";
            return RedirectToPage("./Index");
        }

        InboundOrder = new CreateInboundOrderDto
        {
            ExpectedReceiptDate = DateOnly.FromDateTime(DateTime.Today),
            SourceType = salesOrderId.HasValue
                ? "SALES_RETURN"
                : "PURCHASE_ORDER",
            PurchaseOrderId = purchaseOrderId,
            SalesOrderId = salesOrderId
        };

        await LoadDropdowns();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string actionType)
    {
        InboundOrder.IsSubmit = true;

        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return Page();
        }

        try
        {
            var newOrderId = await _inboundApiService.CreateInboundOrderAsync(InboundOrder);
            TempData["SuccessMessage"] = "Đã ghi nhận số lượng thực nhận. Hãy xác nhận vị trí cất hàng.";
            var order = await _inboundApiService.GetInboundOrderByIdAsync(newOrderId);
            var firstReceipt = order?.Items
                .SelectMany(item => item.Receipts.Select(receipt => new { Item = item, Receipt = receipt }))
                .FirstOrDefault(entry => entry.Receipt.ConditionStatus == "GOOD" &&
                                         entry.Receipt.ReceivedQuantity > entry.Receipt.PutawayQuantity);
            return firstReceipt == null
                ? RedirectToPage("./Details", new { id = newOrderId })
                : RedirectToPage("./Putaway", new
                {
                    id = newOrderId,
                    itemId = firstReceipt.Item.InboundOrderItemId,
                    lotId = firstReceipt.Receipt.ProductLotId
                });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tạo lệnh nhập: " + ex.Message);
            await LoadDropdowns();
            return Page();
        }
    }

    private async Task LoadDropdowns()
    {
        try
        {
            PurchaseOrderSources = await _inboundApiService.GetPurchaseOrderInboundSourcesAsync();
        }
        catch { }

        try
        {
            var returnableSos = await _inboundApiService.GetReturnableSalesOrdersAsync();
            SalesOrders = new SelectList(returnableSos, "Id", "Name");
        }
        catch { }
    }

    public async Task<IActionResult> OnGetPoDetailsAsync(long poId)
    {
        var result = await _inboundApiService.GetPurchaseOrderForInboundAsync(poId);
        if (result == null) return NotFound();
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnGetSoDetailsAsync(long soId)
    {
        var result = await _inboundApiService.GetSalesOrderForInboundAsync(soId);
        if (result == null) return NotFound();
        return new JsonResult(result);
    }

}
