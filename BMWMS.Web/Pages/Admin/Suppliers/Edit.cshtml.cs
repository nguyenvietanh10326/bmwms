using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Suppliers;

public class EditModel : PageModel
{
    private readonly SupplierApiService _supplierApiService;

    public EditModel(SupplierApiService supplierApiService)
    {
        _supplierApiService = supplierApiService;
    }

    [BindProperty]
    public SupplierUpdateRequestModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string supplierCode)
    {
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

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string supplierCode)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _supplierApiService.UpdateSupplierAsync(supplierCode, Input);
        
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Cập nhật thông tin nhà cung cấp thành công.";
            return RedirectToPage("/Admin/Suppliers/Detail", new { supplierCode = Input.SupplierCode });
        }
        else
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return Page();
        }
    }
}
