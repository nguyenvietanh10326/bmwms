using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Suppliers;

public class CreateModel : PageModel
{
    private readonly SupplierApiService _supplierApiService;
    private readonly ProductApiService _productApiService;

    public CreateModel(SupplierApiService supplierApiService, ProductApiService productApiService)
    {
        _supplierApiService = supplierApiService;
        _productApiService = productApiService;
    }

    [BindProperty]
    public SupplierCreateRequestModel Input { get; set; } = new();

    [BindProperty]
    public List<long> SelectedProductIds { get; set; } = new();

    public List<ProductResponseModel> AllProducts { get; set; } = new();

    private async Task LoadProductsAsync()
    {
        var productResult = await _productApiService.GetPagedListAsync(new ProductFilterModel { Status = "ACTIVE", PageIndex = 1, PageSize = 1000 });
        if (productResult != null && productResult.Items != null)
        {
            AllProducts = productResult.Items;
        }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var roleCode = HttpContext.Session.GetString("RoleCode");
        if (roleCode != "SYSTEM_ADMIN" && roleCode != "WAREHOUSE_MANAGER" && roleCode != "PURCHASING_STAFF")
        {
            return Forbid();
        }

        await LoadProductsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var roleCode = HttpContext.Session.GetString("RoleCode");
        if (roleCode != "SYSTEM_ADMIN" && roleCode != "WAREHOUSE_MANAGER" && roleCode != "PURCHASING_STAFF")
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await LoadProductsAsync();
            return Page();
        }

        var result = await _supplierApiService.CreateSupplierAsync(Input);
        
        if (result.Success)
        {
            if (SelectedProductIds != null && SelectedProductIds.Any())
            {
                await _supplierApiService.AssignProductsAsync(Input.SupplierCode, SelectedProductIds);
            }
            TempData["SuccessMessage"] = "Tạo nhà cung cấp thành công.";
            return RedirectToPage("/Admin/Suppliers/Detail", new { supplierCode = Input.SupplierCode });
        }
        else
        {
            await LoadProductsAsync();
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return Page();
        }
    }
}
