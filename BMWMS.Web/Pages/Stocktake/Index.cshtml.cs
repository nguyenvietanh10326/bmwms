using Microsoft.AspNetCore.Authorization;
using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Stocktake
{
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
    public class IndexModel : PageModel
    {
        private readonly IStocktakeApiService _stocktakeService;
        private readonly WarehouseApiService _warehouseService;

        public IndexModel(IStocktakeApiService stocktakeService, WarehouseApiService warehouseService)
        {
            _stocktakeService = stocktakeService;
            _warehouseService = warehouseService;
        }

        public StocktakeSessionPagedResultModel PagedResult { get; set; } = new();
        public List<WarehouseModel> Warehouses { get; set; } = new();
        public bool IsManager { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Keyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? WarehouseId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 15;

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            CheckRole();
            Warehouses = await _warehouseService.GetWarehousesAsync();

            PagedResult = await _stocktakeService.GetSessionsAsync(new StocktakeFilterModel
            {
                Keyword = Keyword,
                Status = Status,
                WarehouseId = WarehouseId,
                FromDate = FromDate,
                ToDate = ToDate,
                PageIndex = PageIndex < 1 ? 1 : PageIndex,
                PageSize = PageSize
            });

            return Page();
        }

        private void CheckRole()
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? string.Empty;
            IsManager = roleCode == "SYSTEM_ADMIN" || roleCode == "WAREHOUSE_MANAGER" || roleCode.Contains("MANAGER") || roleCode.Contains("ADMIN");
        }
    }
}

