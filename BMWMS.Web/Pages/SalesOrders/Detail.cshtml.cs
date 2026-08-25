using Microsoft.AspNetCore.Authorization;
using BMWMS.Web.Models.Inventory;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.SalesOrders
{
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF")]
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
                TempData["ErrorMessage"] = "KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n bÃ¡n hÃ ng.";
                return RedirectToPage("./Index");
            }

            return Page();
        }
    }
}

