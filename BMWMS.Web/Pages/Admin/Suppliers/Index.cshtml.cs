using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Suppliers;

public class IndexModel : PageModel
{
    private readonly SupplierApiService _supplierApiService;

    public IndexModel(SupplierApiService supplierApiService)
    {
        _supplierApiService = supplierApiService;
    }

    [BindProperty(SupportsGet = true)]
    public SupplierFilterModel Filter { get; set; } = new();

    public PagedResultModel<SupplierListResponseModel> Suppliers { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var roleCode = HttpContext.Session.GetString("RoleCode");
        if (roleCode != "SYSTEM_ADMIN" && roleCode != "WAREHOUSE_MANAGER" && roleCode != "PURCHASING_STAFF")
        {
            return Forbid();
        }

        if (Filter.PageIndex < 1) Filter.PageIndex = 1;
        if (Filter.PageSize < 1) Filter.PageSize = 10;

        var result = await _supplierApiService.GetPagedListAsync(Filter);
        if (result != null)
        {
            Suppliers = result;
        }

        return Page();
    }
}
