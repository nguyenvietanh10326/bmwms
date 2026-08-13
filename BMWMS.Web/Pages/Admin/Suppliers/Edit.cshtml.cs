using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Suppliers;

public class EditModel : PageModel
{
    private readonly SupplierApiService _supplierApiService;
    private readonly ProductApiService _productApiService;

    public EditModel(SupplierApiService supplierApiService, ProductApiService productApiService)
    {
        _supplierApiService = supplierApiService;
        _productApiService = productApiService;
    }

    [BindProperty]
    public SupplierUpdateRequestModel Input { get; set; } = new();

    [BindProperty]
    public List<long> SelectedProductIds { get; set; } = new();

    public List<ProductResponseModel> AllProducts { get; set; } = new();

    private async Task LoadProductsAsync(string supplierCode)
    {
        var productResult = await _productApiService.GetPagedListAsync(new ProductFilterModel { Status = "ACTIVE", PageIndex = 1, PageSize = 1000 });
        if (productResult != null && productResult.Items != null)
        {
            AllProducts = productResult.Items;
        }

        var supplier = await _supplierApiService.GetSupplierDetailAsync(supplierCode);
        if (supplier?.SuppliedProducts != null)
        {
            var suppliedCodes = supplier.SuppliedProducts.Select(x => x.ProductCode).ToList();
            SelectedProductIds = AllProducts.Where(p => suppliedCodes.Contains(p.ProductCode)).Select(p => p.ProductId).ToList();
        }
    }

    public async Task<IActionResult> OnGetAsync(string supplierCode)
    {
        var roleCode = HttpContext.Session.GetString("RoleCode");
        if (roleCode != "SYSTEM_ADMIN" && roleCode != "WAREHOUSE_MANAGER" && roleCode != "PURCHASING_STAFF")
        {
            return Forbid();
        }

        var supplier = await _supplierApiService.GetSupplierDetailAsync(supplierCode);
        if (supplier == null)
        {
            return NotFound();
        }

        if (supplier.Status != "ACTIVE" && supplier.Status != "INACTIVE")
        {
            TempData["ErrorMessage"] = "Không thể chỉnh sửa nhà cung cấp ở trạng thái hiện tại.";
            return RedirectToPage("/Admin/Suppliers/Detail", new { supplierCode });
        }

        Input = new SupplierUpdateRequestModel
        {
            SupplierCode = supplier.SupplierCode,
            SupplierName = supplier.SupplierName,
            TaxCode = supplier.TaxCode,
            PhoneNumber = supplier.PhoneNumber,
            Email = supplier.Email,
            Address = supplier.Address,
            RepresentativeName = supplier.RepresentativeName,
            Status = supplier.Status
        };

        await LoadProductsAsync(supplierCode);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string supplierCode)
    {
        var roleCode = HttpContext.Session.GetString("RoleCode");
        if (roleCode != "SYSTEM_ADMIN" && roleCode != "WAREHOUSE_MANAGER" && roleCode != "PURCHASING_STAFF")
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await LoadProductsAsync(supplierCode);
            return Page();
        }

        var result = await _supplierApiService.UpdateSupplierAsync(supplierCode, Input);
        
        if (result.Success)
        {
            if (SelectedProductIds != null)
            {
                await _supplierApiService.AssignProductsAsync(supplierCode, SelectedProductIds);
            }
            TempData["SuccessMessage"] = "Cập nhật thông tin nhà cung cấp thành công.";
            return RedirectToPage("/Admin/Suppliers/Detail", new { supplierCode });
        }
        else
        {
            await LoadProductsAsync(supplierCode);
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return Page();
        }
    }
}
