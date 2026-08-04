using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMWMS.Web.Pages.Admin.Suppliers;

public class InboundHistoryModel : PageModel
{
    private readonly SupplierApiService _supplierApiService;
    private readonly WarehouseApiService _warehouseApiService;

    public InboundHistoryModel(SupplierApiService supplierApiService, WarehouseApiService warehouseApiService)
    {
        _supplierApiService = supplierApiService;
        _warehouseApiService = warehouseApiService;
    }

    [BindProperty(SupportsGet = true)]
    public string SupplierCode { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public SupplierInboundHistoryFilterModel Filter { get; set; } = new SupplierInboundHistoryFilterModel();

    public List<SupplierInboundHistoryResponseModel> Inbounds { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Filter.PageSize);

    public SelectList? WarehouseOptions { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Get dropdown options
        var warehouses = await _warehouseApiService.GetWarehousesAsync();
        WarehouseOptions = new SelectList(warehouses, "WarehouseId", "WarehouseName", Filter.WarehouseId);

        // Get inbounds
        var result = await _supplierApiService.GetSupplierInboundHistoryAsync(SupplierCode, Filter);
        if (result != null)
        {
            Inbounds = result.Items.ToList();
            TotalCount = result.TotalCount;
        }

        return Page();
    }

    public async Task<IActionResult> OnGetDetailAsync(string orderNumber)
    {
        var detail = await _supplierApiService.GetSupplierInboundHistoryDetailAsync(SupplierCode, orderNumber);
        if (detail == null) return NotFound();
        return new JsonResult(detail);
    }
}
