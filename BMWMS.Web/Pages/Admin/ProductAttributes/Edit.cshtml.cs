using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace BMWMS.Web.Pages.Admin.ProductAttributes
{
    public class EditModel : PageModel
    {
        private readonly ProductAttributeApiService _apiService;

        public EditModel(ProductAttributeApiService apiService)
        {
            _apiService = apiService;
        }

        [BindProperty]
        public UpdateProductAttributeDto Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public long Id { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var attribute = await _apiService.GetByIdAsync(Id);
            if (attribute == null)
            {
                return NotFound();
            }

            Input = new UpdateProductAttributeDto
            {
                AttributeCode = attribute.AttributeCode,
                AttributeName = attribute.AttributeName,
                DataType = attribute.DataType,
                UnitLabel = attribute.UnitLabel,
                Description = attribute.Description,
                Status = attribute.Status,
                Options = attribute.Options.Select(o => new ProductAttributeOptionDto
                {
                    ProductAttributeOptionId = o.ProductAttributeOptionId,
                    ProductAttributeId = o.ProductAttributeId,
                    OptionCode = o.OptionCode,
                    OptionValue = o.OptionValue,
                    DisplayOrder = o.DisplayOrder,
                    IsActive = o.IsActive
                }).ToList()
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
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

                await _apiService.UpdateAsync(Id, Input);
                TempData["SuccessMessage"] = "Đã cập nhật Thông số kỹ thuật thành công.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            try
            {
                await _apiService.DeleteAsync(Id);
                TempData["SuccessMessage"] = "Đã xóa Thông số kỹ thuật thành công.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage("./Index");
            }
        }
    }
}
