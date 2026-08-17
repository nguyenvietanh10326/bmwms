using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Suppliers;

public class DetailModel : PageModel
{
    private readonly SupplierApiService _supplierApiService;
    private readonly ProductApiService _productApiService;

    public DetailModel(SupplierApiService supplierApiService, ProductApiService productApiService)
    {
        _supplierApiService = supplierApiService;
        _productApiService = productApiService;
    }

    public SupplierDetailResponseModel Supplier { get; set; } = null!;
    public List<ProductResponseModel> AllProducts { get; set; } = new();

    [BindProperty]
    public List<long> SelectedProductIds { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string supplierCode)
    {
        var roleCode = HttpContext.Session.GetString("RoleCode");
        if (roleCode != "SYSTEM_ADMIN" && roleCode != "WAREHOUSE_MANAGER" && roleCode != "PURCHASING_STAFF")
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(supplierCode))
        {
            return RedirectToPage("/Admin/Suppliers/Index");
        }

        var result = await _supplierApiService.GetSupplierDetailAsync(supplierCode);
        
        if (result == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy nhà cung cấp hoặc bạn không có quyền xem.";
            return RedirectToPage("/Admin/Suppliers/Index");
        }

        Supplier = result;

        return Page();
    }

    public async Task<IActionResult> OnPostSuspendAsync(string supplierCode)
    {
        var roleCode = HttpContext.Session.GetString("RoleCode");
        if (roleCode != "SYSTEM_ADMIN" && roleCode != "WAREHOUSE_MANAGER" && roleCode != "PURCHASING_STAFF")
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(supplierCode)) return RedirectToPage("/Admin/Suppliers/Index");

        var supplier = await _supplierApiService.GetSupplierDetailAsync(supplierCode);
        if (supplier == null) return RedirectToPage("/Admin/Suppliers/Index");

        var updateRequest = new SupplierUpdateRequestModel
        {
            SupplierCode = supplier.SupplierCode,
            SupplierName = supplier.SupplierName,
            TaxCode = supplier.TaxCode,
            PhoneNumber = supplier.PhoneNumber,
            Email = supplier.Email,
            Address = supplier.Address,
            RepresentativeName = supplier.RepresentativeName,
            Status = "INACTIVE"
        };

        var (success, error) = await _supplierApiService.UpdateSupplierAsync(supplierCode, updateRequest);

        if (!success)
        {
            TempData["ErrorMessage"] = error;
        }
        else
        {
            TempData["SuccessMessage"] = "Đã ngừng giao dịch với nhà cung cấp thành công.";
        }

        return RedirectToPage(new { supplierCode });
    }
}
