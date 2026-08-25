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

[Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
public class CreateModel : PageModel
{
    private readonly InboundApiService _inboundApiService;
    private readonly ProductApiService _productApiService;

    public CreateModel(
        InboundApiService inboundApiService,
        ProductApiService productApiService)
    {
        _inboundApiService = inboundApiService;
        _productApiService = productApiService;
    }

    [BindProperty]
    public CreateInboundOrderDto InboundOrder { get; set; } = new();

    public SelectList Products { get; set; } = new(Array.Empty<object>());
    public SelectList Users { get; set; } = new(Array.Empty<object>());
    public SelectList PurchaseOrders { get; set; } = new(Array.Empty<object>());
    public SelectList SalesOrders { get; set; } = new(Array.Empty<object>());

    public async Task OnGetAsync(long? purchaseOrderId, long? salesOrderId)
    {
        InboundOrder = new CreateInboundOrderDto
        {
            ExpectedReceiptDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            SourceType = salesOrderId.HasValue ? "SALES_RETURN" : "PURCHASE_ORDER",
            PurchaseOrderId = purchaseOrderId,
            SalesOrderId = salesOrderId
        };

        await LoadDropdowns();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdowns();
            return Page();
        }

        try
        {
            var newOrderId = await _inboundApiService.CreateInboundOrderAsync(InboundOrder);
            TempData["SuccessMessage"] = "Lệnh nhập kho đã được tạo thành công.";
            return RedirectToPage("./Details", new { id = newOrderId });
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
            var productResult = await _productApiService.GetPagedListAsync(new ProductFilterModel { PageSize = 1000 });
            Products = new SelectList(productResult.Items, "ProductId", "ProductName");
        }
        catch { }

        try
        {
            var warehouseUsers = await _inboundApiService.GetAvailableWarehouseStaffAsync();
            Users = new SelectList(warehouseUsers, "UserId", "FullName");
        }
        catch { }

        try
        {
            var pendingPos = await _inboundApiService.GetPendingPurchaseOrdersAsync();
            PurchaseOrders = new SelectList(pendingPos, "Id", "Name");
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
