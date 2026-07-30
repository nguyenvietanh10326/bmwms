using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Admin.Suppliers;

public class CreateModel : PageModel
{
    private readonly SupplierApiService _supplierApiService;

    public CreateModel(SupplierApiService supplierApiService)
    {
        _supplierApiService = supplierApiService;
    }

    [BindProperty]
    public SupplierCreateRequestModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var result = await _supplierApiService.CreateSupplierAsync(Input);
        
        if (result.Success)
        {
            TempData["SuccessMessage"] = "Tạo nhà cung cấp thành công.";
            return RedirectToPage("/Admin/Suppliers/Detail", new { supplierCode = Input.SupplierCode });
        }
        else
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return Page();
        }
    }
}
