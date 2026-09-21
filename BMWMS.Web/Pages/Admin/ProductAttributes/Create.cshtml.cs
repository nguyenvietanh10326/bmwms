using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace BMWMS.Web.Pages.Admin.ProductAttributes
{
    public class CreateModel : PageModel
    {
        private readonly ProductAttributeApiService _apiService;

        public CreateModel(ProductAttributeApiService apiService)
        {
            _apiService = apiService;
        }

        [BindProperty]
        public CreateProductAttributeDto Input { get; set; } = new();

        public IActionResult OnGet()
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? "";
            if (roleCode is not ("SYSTEM_ADMIN" or "WAREHOUSE_MANAGER"))
            {
                TempData["WarningMessage"] = "Bạn không có quyền thêm mới thông số kỹ thuật! Chỉ Quản trị hệ thống hoặc Trưởng kho mới được thao tác.";
                return RedirectToPage("./Index");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? "";
            if (roleCode is not ("SYSTEM_ADMIN" or "WAREHOUSE_MANAGER"))
            {
                TempData["WarningMessage"] = "Bạn không có quyền thêm mới thông số kỹ thuật! Chỉ Quản trị hệ thống hoặc Trưởng kho mới được thao tác.";
                return RedirectToPage("./Index");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                if (Input.DataType != "OPTION")
                {
                    Input.Options.Clear();
                }
                else
                {
                    Input.Options = Input.Options.Where(o => !string.IsNullOrWhiteSpace(o.OptionCode) && !string.IsNullOrWhiteSpace(o.OptionValue)).ToList();
                    if (!Input.Options.Any())
                    {
                        ModelState.AddModelError("", "Vui lòng nhập ít nhất một lựa chọn (Option) khi kiểu dữ liệu là OPTION.");
                        return Page();
                    }
                }

                await _apiService.CreateAsync(Input);
                TempData["SuccessMessage"] = "Đã thêm mới Thông số kỹ thuật thành công.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }
    }
}
