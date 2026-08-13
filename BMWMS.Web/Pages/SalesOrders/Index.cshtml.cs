using BMWMS.Web.Models.Inventory;
using BMWMS.Web.Models.Warehouse;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.SalesOrders
{
    public class IndexModel : PageModel
    {
        private readonly SalesOrderApiService _apiService;

        public IndexModel(SalesOrderApiService apiService)
        {
            _apiService = apiService;
        }

        [BindProperty(SupportsGet = true)]
        public SalesOrderFilterDto Filter { get; set; } = new();

        public PagedResultDto<SalesOrderListDto> Result { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            Result = await _apiService.GetPagedOrdersAsync(Filter);
            return Page();
        }
    }
}
