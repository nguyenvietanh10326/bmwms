using BMWMS.Web.Models.Inventory;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.SalesOrders
{
    public class DetailModel : PageModel
    {
        private readonly SalesOrderApiService _apiService;

        public DetailModel(SalesOrderApiService apiService)
        {
            _apiService = apiService;
        }

        public SalesOrderDetailDto? Order { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            if (id <= 0) return RedirectToPage("./Index");

            Order = await _apiService.GetOrderDetailAsync(id);

            if (Order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn bán hàng.";
                return RedirectToPage("./Index");
            }

            return Page();
        }
    }
}
