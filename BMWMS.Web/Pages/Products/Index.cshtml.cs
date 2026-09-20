using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.Products
{
    public class IndexModel : PageModel
    {
        private readonly ProductApiService _productApiService;

        public IndexModel(ProductApiService productApiService)
        {
            _productApiService = productApiService;
        }

        [BindProperty(SupportsGet = true)]
        public ProductFilterModel Filter { get; set; } = new();

        public PagedResultModel<ProductResponseModel> ProductsResult { get; set; } = new();
        public List<SelectListItem> GroupOptions { get; set; } = new();
        public List<SelectListItem> UomOptions { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }
        [TempData]
        public string? ErrorMessage { get; set; }
        [TempData]
        public string? WarningMessage { get; set; }

        public bool CanManage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? "";
            CanManage = roleCode is "SYSTEM_ADMIN" or "WAREHOUSE_MANAGER";

            if (Filter.PageIndex < 1) Filter.PageIndex = 1;
            if (Filter.PageSize < 1) Filter.PageSize = 10;

            await LoadDropdownsAsync();
            ProductsResult = await _productApiService.GetPagedListAsync(Filter);
            return Page();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(long id)
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? "";
            if (roleCode is not ("SYSTEM_ADMIN" or "WAREHOUSE_MANAGER"))
            {
                WarningMessage = "Bạn không có quyền thay đổi trạng thái sản phẩm! Chỉ Quản trị hệ thống hoặc Trưởng kho mới được thực hiện.";
                return RedirectToPage("./Index", new
                {
                    Filter.Keyword,
                    Filter.ProductGroupId,
                    Filter.UnitOfMeasureId,
                    Filter.Status,
                    Filter.RotationMethod,
                    Filter.PageIndex,
                    Filter.PageSize
                });
            }

            var (success, msg) = await _productApiService.ToggleStatusAsync(id);
            if (success)
            {
                SuccessMessage = "Thay đổi trạng thái sản phẩm thành công!";
            }
            else
            {
                ErrorMessage = msg;
            }

            return RedirectToPage("./Index", new
            {
                Filter.Keyword,
                Filter.ProductGroupId,
                Filter.UnitOfMeasureId,
                Filter.Status,
                Filter.RotationMethod,
                Filter.PageIndex,
                Filter.PageSize
            });
        }

        private async Task LoadDropdownsAsync()
        {
            var groups = await _productApiService.GetProductGroupsAsync();
            GroupOptions = new List<SelectListItem> { new("--- Tất cả nhóm hàng ---", "") };
            foreach (var g in groups)
            {
                GroupOptions.Add(new SelectListItem(g.GroupName, g.ProductGroupId.ToString()));
            }

            var uoms = await _productApiService.GetUnitsOfMeasureAsync();
            UomOptions = new List<SelectListItem> { new("--- Tất cả ĐVT ---", "") };
            foreach (var u in uoms)
            {
                UomOptions.Add(new SelectListItem($"{u.UnitName} ({u.UnitCode})", u.UnitOfMeasureId.ToString()));
            }
        }
    }
}
