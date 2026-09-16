using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Suppliers;

public class InboundHistoryModel : PageModel
{
    private readonly SupplierApiService _supplierApiService;

    public InboundHistoryModel(SupplierApiService supplierApiService)
    {
        _supplierApiService = supplierApiService;
    }

    [BindProperty(SupportsGet = true)]
    public string SupplierCode { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public SupplierInboundHistoryFilterModel Filter { get; set; } = new SupplierInboundHistoryFilterModel();

    public List<SupplierInboundHistoryResponseModel> Inbounds { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Filter.PageSize);

    public async Task<IActionResult> OnGetAsync()
    {
        var roleCode = HttpContext.Session.GetString("RoleCode");
        if (roleCode != "SYSTEM_ADMIN" && roleCode != "WAREHOUSE_MANAGER" && roleCode != "PURCHASING_STAFF" && roleCode != "ACCOUNTANT" && roleCode != "DIRECTOR")
        {
            return Forbid();
        }

        Filter.PageIndex = Math.Max(1, Filter.PageIndex);
        Filter.PageSize = Math.Clamp(Filter.PageSize, 1, 100);
        if (Filter.FromDate > Filter.ToDate) { TempData["ErrorMessage"] = "Từ ngày không được sau đến ngày."; return Page(); }
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
