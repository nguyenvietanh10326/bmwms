using Microsoft.AspNetCore.Authorization;
using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.Products
{
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,PURCHASING_STAFF,SALES_STAFF")]
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

        public async Task<IActionResult> OnGetAsync()
        {
            if (Filter.PageIndex < 1) Filter.PageIndex = 1;
            if (Filter.PageSize < 1) Filter.PageSize = 10;

            await LoadDropdownsAsync();
            ProductsResult = await _productApiService.GetPagedListAsync(Filter);
            return Page();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(long id)
        {
            var (success, msg) = await _productApiService.ToggleStatusAsync(id);
            if (success)
            {
                SuccessMessage = "Thay Ä‘á»•i tráº¡ng thÃ¡i sáº£n pháº©m thÃ nh cÃ´ng!";
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
            GroupOptions = new List<SelectListItem> { new("--- Táº¥t cáº£ nhÃ³m hÃ ng ---", "") };
            foreach (var g in groups)
            {
                GroupOptions.Add(new SelectListItem(g.GroupName, g.ProductGroupId.ToString()));
            }

            var uoms = await _productApiService.GetUnitsOfMeasureAsync();
            UomOptions = new List<SelectListItem> { new("--- Táº¥t cáº£ ÄVT ---", "") };
            foreach (var u in uoms)
            {
                UomOptions.Add(new SelectListItem($"{u.UnitName} ({u.UnitCode})", u.UnitOfMeasureId.ToString()));
            }
        }
    }
}

