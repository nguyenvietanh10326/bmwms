using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Suppliers;

public class ProductsModel : PageModel
{
    private readonly SupplierApiService _supplierApiService;

    public ProductsModel(SupplierApiService supplierApiService)
    {
        _supplierApiService = supplierApiService;
    }

    public string SupplierCode { get; set; } = null!;
    
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResultModel<SupplierProductResponseDto>? Products { get; set; }

    public async Task<IActionResult> OnGetAsync(string supplierCode)
    {
        var roleCode = HttpContext.Session.GetString("RoleCode");
        if (roleCode != "SYSTEM_ADMIN" && roleCode != "WAREHOUSE_MANAGER" && roleCode != "PURCHASING_STAFF")
        {
            return Forbid();
        }

        SupplierCode = supplierCode;

        Products = await _supplierApiService.GetSupplierProductsAsync(supplierCode, Search, Status, PageNumber, 20);

        if (Products == null)
        {
            return RedirectToPage("/Admin/Suppliers/Index");
        }

        return Page();
    }
}
