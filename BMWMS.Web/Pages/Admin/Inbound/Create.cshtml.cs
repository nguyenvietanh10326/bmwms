using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMWMS.Web.Pages.Admin.Inbound;

public class CreateModel : PageModel
{
    private readonly InboundApiService _inboundApiService;
    private readonly WarehouseApiService _warehouseApiService;
    private readonly ProductApiService _productApiService;
    private readonly UserApiService _userApiService;

    public CreateModel(
        InboundApiService inboundApiService,
        WarehouseApiService warehouseApiService,
        ProductApiService productApiService,
        UserApiService userApiService)
    {
        _inboundApiService = inboundApiService;
        _warehouseApiService = warehouseApiService;
        _productApiService = productApiService;
        _userApiService = userApiService;
    }

    [BindProperty]
    public CreateInboundOrderDto InboundOrder { get; set; } = new();

    public SelectList Warehouses { get; set; } = default!;
    public SelectList Products { get; set; } = default!;
    public SelectList Users { get; set; } = default!;
    public SelectList ShortageOrders { get; set; } = default!;
    public SelectList PurchaseOrders { get; set; } = default!;
    public SelectList SalesOrders { get; set; } = default!;

    public async Task OnGetAsync()
    {
        InboundOrder = new CreateInboundOrderDto
        {
            ExpectedReceiptDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1))
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
        var warehouses = await _warehouseApiService.GetWarehousesAsync();
        Warehouses = new SelectList(warehouses, "WarehouseId", "WarehouseName");

        var productResult = await _productApiService.GetPagedListAsync(new ProductFilterModel { PageSize = 1000 });
        Products = new SelectList(productResult.Items, "ProductId", "ProductName");

        var usersResult = await _userApiService.GetUsersAsync(new UserFilterModel { PageSize = 1000 });
        
        var allUsers = usersResult?.Items ?? new List<UserListItem>();
        var warehouseUsers = allUsers.Where(u => 
            (u.RoleName.Contains("Kho", StringComparison.OrdinalIgnoreCase) || 
             u.RoleName.Contains("Warehouse", StringComparison.OrdinalIgnoreCase)) &&
            u.Status == "ACTIVE").ToList();

        Users = new SelectList(warehouseUsers, "UserId", "FullName");

        var shortages = await _inboundApiService.GetShortageInboundOrdersAsync();
        ShortageOrders = new SelectList(shortages.Select(s => new {
            Id = s.InboundOrderId,
            Name = $"{s.PurchaseOrderNumber} (từ lệnh thiếu {s.InboundOrderNumber}) - {s.SupplierName}"
        }), "Id", "Name");

        var pendingPos = await _inboundApiService.GetPendingPurchaseOrdersAsync();
        PurchaseOrders = new SelectList(pendingPos, "Id", "Name");

        var returnableSos = await _inboundApiService.GetReturnableSalesOrdersAsync();
        SalesOrders = new SelectList(returnableSos, "Id", "Name");
    }

    public async Task<IActionResult> OnGetPoDetailsAsync(long poId)
    {
        var result = await _inboundApiService.GetPurchaseOrderForInboundAsync(poId);
        if (result == null) return NotFound();
        return new JsonResult(result);
    }

    public async Task<IActionResult> OnGetSupplementDetailsAsync(long parentId)
    {
        var result = await _inboundApiService.GetInboundOrderForSupplementAsync(parentId);
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
