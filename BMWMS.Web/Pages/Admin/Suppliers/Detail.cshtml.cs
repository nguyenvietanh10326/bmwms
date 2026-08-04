using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Suppliers;

public class DetailModel : PageModel
{
    private readonly SupplierApiService _supplierApiService;

    public DetailModel(SupplierApiService supplierApiService)
    {
        _supplierApiService = supplierApiService;
    }

    public SupplierDetailResponseModel Supplier { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(string supplierCode)
    {
        if (string.IsNullOrWhiteSpace(supplierCode))
        {
            return RedirectToPage("/Admin/Suppliers/Index");
        }

        var result = await _supplierApiService.GetSupplierDetailAsync(supplierCode);
        
        if (result == null)
        {
            // Trở về danh sách kèm thông báo (có thể dùng TempData)
            TempData["ErrorMessage"] = "Không tìm thấy nhà cung cấp hoặc bạn không có quyền xem.";
            return RedirectToPage("/Admin/Suppliers/Index");
        }

        Supplier = result;
        return Page();
    }

    public async Task<IActionResult> OnPostSuspendAsync(string supplierCode)
    {
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
