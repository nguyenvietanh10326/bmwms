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
}
