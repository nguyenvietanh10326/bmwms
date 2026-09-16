using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BMWMS.Web.Services;
using static BMWMS.Web.Services.TransferApiService;

namespace BMWMS.Web.Pages.Transfer
{
    public class IndexModel : PageModel
    {
        private readonly TransferApiService _transferSvc;

        public IndexModel(TransferApiService transferSvc)
        {
            _transferSvc = transferSvc;
        }

        public TransferOrderPagedResultDto PagedResult { get; set; } = new();
        public bool IsManager { get; set; } = false;

        [BindProperty(SupportsGet = true)]
        public string? Keyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 15;

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            CheckUserRole();

            var filter = new TransferOrderFilterDto
            {
                Keyword = Keyword,
                Status = Status,
                WarehouseId = 1,
                PageIndex = PageIndex < 1 ? 1 : PageIndex,
                PageSize = PageSize
            };

            PagedResult = await _transferSvc.GetOrdersAsync(filter);
            return Page();
        }

        private void CheckUserRole()
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpper() ?? "";
            IsManager = roleCode.Contains("ADMIN") || roleCode.Contains("MANAGER") || roleCode == "WAREHOUSE_MANAGER";
        }
    }
}
