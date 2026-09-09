using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.Products
{
    public class EditModel : PageModel
    {
        private readonly ProductApiService _productApiService;
        private readonly ProductGroupApiService _productGroupApiService;

        public EditModel(ProductApiService productApiService, ProductGroupApiService productGroupApiService)
        {
            _productApiService = productApiService;
            _productGroupApiService = productGroupApiService;
        }

        [BindProperty(SupportsGet = true)]
        public long Id { get; set; }

        [BindProperty]
        public UpdateProductModel Product { get; set; } = new();

        public List<SelectListItem> GroupOptions { get; set; } = new();
        public List<SelectListItem> UomOptions { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Id = id;
            var detail = await _productApiService.GetByIdAsync(id);
            if (detail == null)
            {
                TempData["ErrorMessage"] = $"Không tìm thấy sản phẩm có ID = {id}.";
                return RedirectToPage("./Index");
            }

            Product = new UpdateProductModel
            {
                ProductCode = detail.ProductCode,
                ProductName = detail.ProductName,
                ProductGroupId = detail.ProductGroupId,
                UnitOfMeasureId = detail.UnitOfMeasureId,
                Barcode = detail.Barcode,
                Description = detail.Description,
                RotationMethod = detail.RotationMethod,
                TrackExpiry = detail.TrackExpiry,
                DefaultShelfLifeDays = detail.DefaultShelfLifeDays,
                Status = detail.Status,
                AttributeValues = detail.AttributeValues?.Select(av => new ProductAttributeValueModel
                {
                    ProductAttributeId = av.ProductAttributeId,
                    AttributeCode = av.AttributeCode,
                    AttributeName = av.AttributeName,
                    DataType = av.DataType,
                    UnitLabel = av.UnitLabel,
                    AttributeValue = av.AttributeValue,
                    DisplayOrder = av.DisplayOrder
                }).ToList() ?? new()
            };

            await LoadDropdownsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return Page();
            }

            var (success, message) = await _productApiService.UpdateAsync(Id, Product);
            if (success)
            {
                TempData["SuccessMessage"] = "Cập nhật thông tin hàng hóa thành công!";
                return RedirectToPage("./Index");
            }

            ErrorMessage = message;
            await LoadDropdownsAsync();
            return Page();
        }

        private async Task LoadDropdownsAsync()
        {
            var groups = await _productApiService.GetProductGroupsAsync();
            GroupOptions = new List<SelectListItem> { new("--- Chọn nhóm danh mục ---", "") };
            foreach (var g in groups)
            {
                GroupOptions.Add(new SelectListItem(g.GroupName, g.ProductGroupId.ToString(), g.ProductGroupId == Product.ProductGroupId));
            }

            var uoms = await _productApiService.GetUnitsOfMeasureAsync();
            UomOptions = new List<SelectListItem> { new("--- Chọn đơn vị tính cơ sở ---", "") };
            foreach (var u in uoms)
            {
                UomOptions.Add(new SelectListItem($"{u.UnitName} ({u.UnitCode})", u.UnitOfMeasureId.ToString(), u.UnitOfMeasureId == Product.UnitOfMeasureId));
            }
        }

        public async Task<IActionResult> OnGetGroupAttributesAsync(long id)
        {
            var attrs = await _productGroupApiService.GetGroupAttributesAsync(id);
            return new JsonResult(attrs);
        }

        public async Task<IActionResult> OnGetGroupDetailAsync(long id)
        {
            var detail = await _productGroupApiService.GetByIdAsync(id);
            return new JsonResult(detail);
        }
    }
}
