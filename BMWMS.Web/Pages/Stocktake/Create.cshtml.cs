using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Stocktake
{
    public class CreateModel : PageModel
    {
        private readonly IStocktakeApiService _stocktakeService;
        private readonly WarehouseApiService _warehouseService;

        public CreateModel(IStocktakeApiService stocktakeService, WarehouseApiService warehouseService)
        {
            _stocktakeService = stocktakeService;
            _warehouseService = warehouseService;
        }

        public List<WarehouseModel> Warehouses { get; set; } = new();
        public List<StocktakeStaffOptionModel> StaffUsers { get; set; } = new();
        public List<StocktakeLocationOptionModel> Locations { get; set; } = new();

        [BindProperty]
        public long WarehouseId { get; set; }

        [BindProperty]
        public DateOnly PlannedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [BindProperty]
        public long? AssignedToUserId { get; set; }

        [BindProperty]
        public string? Notes { get; set; }

        [BindProperty]
        public List<long> StorageLocationIds { get; set; } = new();

        public bool IsManager { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!CheckManager())
                return RedirectToPage("/Stocktake/Index");

            Warehouses = await _warehouseService.GetWarehousesAsync();
            StaffUsers = await _stocktakeService.GetStaffUsersAsync();
            WarehouseId = Warehouses.FirstOrDefault()?.WarehouseId ?? 0;
            if (WarehouseId > 0)
                Locations = await _stocktakeService.GetLocationsAsync(WarehouseId);

            return Page();
        }

        public async Task<IActionResult> OnGetLocationsByWarehouseAsync(long warehouseId)
        {
            var locations = warehouseId > 0
                ? await _stocktakeService.GetLocationsAsync(warehouseId)
                : new List<StocktakeLocationOptionModel>();

            return new JsonResult(locations);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!CheckManager())
                return RedirectToPage("/Stocktake/Index");

            Warehouses = await _warehouseService.GetWarehousesAsync();
            StaffUsers = await _stocktakeService.GetStaffUsersAsync();
            Locations = WarehouseId > 0 ? await _stocktakeService.GetLocationsAsync(WarehouseId) : new();

            if (WarehouseId <= 0)
            {
                ErrorMessage = "Chon warehouse truoc khi tao dot kiem kho.";
                return Page();
            }

            var result = await _stocktakeService.CreateSessionAsync(new CreateStocktakeSessionModel
            {
                WarehouseId = WarehouseId,
                PlannedDate = PlannedDate,
                AssignedToUserId = AssignedToUserId,
                Notes = Notes,
                StorageLocationIds = StorageLocationIds ?? new()
            });

            if (!result.Success)
            {
                ErrorMessage = result.Message;
                return Page();
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToPage("/Stocktake/Details", new { id = result.StocktakeSessionId });
        }

        private bool CheckManager()
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? string.Empty;
            IsManager = roleCode == "SYSTEM_ADMIN" || roleCode == "WAREHOUSE_MANAGER" || roleCode.Contains("MANAGER") || roleCode.Contains("ADMIN");
            return IsManager;
        }
    }
}
