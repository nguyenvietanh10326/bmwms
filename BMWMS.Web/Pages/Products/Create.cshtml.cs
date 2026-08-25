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
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public class CreateModel : PageModel
    {
        private readonly ProductApiService _productApiService;
        private readonly ProductGroupApiService _productGroupApiService;

        public CreateModel(ProductApiService productApiService, ProductGroupApiService productGroupApiService)
        {
            _productApiService = productApiService;
            _productGroupApiService = productGroupApiService;
        }

        [BindProperty]
        public CreateProductModel Product { get; set; } = new();

        public List<SelectListItem> GroupOptions { get; set; } = new();
        public List<SelectListItem> UomOptions { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
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

            var (success, message) = await _productApiService.CreateAsync(Product);
            if (success)
            {
                TempData["SuccessMessage"] = "ThÃªm má»›i hÃ ng hÃ³a / váº­t tÆ° thÃ nh cÃ´ng!";
                return RedirectToPage("./Index");
            }

            ErrorMessage = message;
            await LoadDropdownsAsync();
            return Page();
        }

        private async Task LoadDropdownsAsync()
        {
            var groups = await _productApiService.GetProductGroupsAsync();
            GroupOptions = new List<SelectListItem> { new("--- Chá»n nhÃ³m danh má»¥c ---", "") };
            foreach (var g in groups)
            {
                GroupOptions.Add(new SelectListItem(g.GroupName, g.ProductGroupId.ToString()));
            }

            var uoms = await _productApiService.GetUnitsOfMeasureAsync();
            UomOptions = new List<SelectListItem> { new("--- Chá»n Ä‘Æ¡n vá»‹ tÃ­nh cÆ¡ sá»Ÿ ---", "") };
            foreach (var u in uoms)
            {
                UomOptions.Add(new SelectListItem($"{u.UnitName} ({u.UnitCode})", u.UnitOfMeasureId.ToString()));
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

