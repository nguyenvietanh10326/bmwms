using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace BMWMS.Web.Pages.Products
{
    public class DetailsModel : PageModel
    {
        private readonly ProductApiService _productApiService;

        public DetailsModel(ProductApiService productApiService)
        {
            _productApiService = productApiService;
        }

        public ProductDetailResponseModel Product { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long id)
        {
            var detail = await _productApiService.GetByIdAsync(id);
            if (detail == null)
            {
                TempData["ErrorMessage"] = $"Không tìm thấy sản phẩm có ID = {id}.";
                return RedirectToPage("./Index");
            }

            Product = detail;
            return Page();
        }
    }
}
