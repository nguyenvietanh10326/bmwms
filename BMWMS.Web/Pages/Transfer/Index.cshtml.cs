using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BMWMS.Web.Services;
using static BMWMS.Web.Services.TransferApiService;

namespace BMWMS.Web.Pages.Transfer
{
    public class IndexModel : PageModel
    {
        private readonly TransferApiService _transferSvc;
        private readonly WarehouseApiService _warehouseSvc;

        public IndexModel(TransferApiService transferSvc, WarehouseApiService warehouseSvc)
        {
            _transferSvc = transferSvc;
            _warehouseSvc = warehouseSvc;
        }

        public TransferOrderPagedResultDto PagedResult { get; set; } = new();
        public List<BMWMS.Web.Models.WarehouseModel> Warehouses { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Keyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public long? WarehouseId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 15;

        // Feedback messages
        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Warehouses = await _warehouseSvc.GetWarehousesAsync();

            var filter = new TransferOrderFilterDto
            {
                Keyword     = Keyword,
                Status      = Status,
                WarehouseId = WarehouseId,
                PageIndex   = PageIndex < 1 ? 1 : PageIndex,
                PageSize    = PageSize
            };

            PagedResult = await _transferSvc.GetOrdersAsync(filter);
            return Page();
        }
    }
}
