using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Stocktake
{
    /// <summary>
    /// BP-05 — Warehouse Staff lane:
    ///   "Physically count each bin" → "Enter actual quantity"
    /// Manager can also view; Submit locks the bin count.
    /// </summary>
    public class CountModel : PageModel
    {
        private readonly IStocktakeApiService _stocktakeService;

        public CountModel(IStocktakeApiService stocktakeService)
        {
            _stocktakeService = stocktakeService;
        }

        public StocktakeCountTaskModel? CountTask { get; set; }
        public bool IsManager { get; set; }
        public bool IsStaff { get; set; }

        [BindProperty(SupportsGet = true)]
        public long Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public long LocationId { get; set; }

        [BindProperty]
        public List<StocktakeCountLineModel> Lines { get; set; } = new();

        // Unexpected item fields
        [BindProperty]
        public long ProductId { get; set; }

        [BindProperty]
        public long ProductLotId { get; set; }

        [BindProperty]
        public decimal UnexpectedQty { get; set; }

        [BindProperty]
        public string? UnexpectedNotes { get; set; }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        // GET: load count task for this session + location
        public async Task<IActionResult> OnGetAsync(long id, long locationId)
        {
            if (id <= 0 || locationId <= 0)
                return RedirectToPage("/Stocktake/Index");

            Id = id;
            LocationId = locationId;
            CheckRole();

            if (!IsStaff)
                return RedirectToPage("/Stocktake/Details", new { id });

            CountTask = await _stocktakeService.GetCountTaskAsync(id, locationId);
            if (CountTask == null)
                return NotFound();

            return Page();
        }

        // GET handler: autocomplete product lots for "Add unexpected item"
        public async Task<IActionResult> OnGetSearchLotsAsync(string? keyword, long id, long locationId)
        {
            var lots = await _stocktakeService.SearchProductLotsAsync(keyword, take: 15);
            return new JsonResult(lots.Select(l => new
            {
                l.ProductId,
                l.ProductLotId,
                l.DisplayLabel,
                ExpiryDate = l.ExpiryDate?.ToString("dd/MM/yyyy"),
                l.OnHandQuantity,
                l.UnitCode
            }));
        }

        // POST: save draft counts (does not lock / submit)
        public async Task<IActionResult> OnPostSaveAsync(long id, long locationId)
        {
            CheckRole();
            if (!IsStaff)
                return Deny(id, locationId, "Bạn không có quyền nhập số liệu kiểm kho.");

            var result = await _stocktakeService.SaveCountsAsync(id, locationId, Lines);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Stocktake/Count", new { id, locationId });
        }

        // POST: submit bin (lock count, trigger variance calculation on API side)
        public async Task<IActionResult> OnPostSubmitAsync(long id, long locationId)
        {
            CheckRole();
            if (!IsStaff)
                return Deny(id, locationId, "Bạn không có quyền submit bin kiểm kho.");

            // Save counts first, then submit
            var saveResult = await _stocktakeService.SaveCountsAsync(id, locationId, Lines);
            if (!saveResult.Success)
            {
                TempData["ErrorMessage"] = saveResult.Message;
                return RedirectToPage("/Stocktake/Count", new { id, locationId });
            }

            var submitResult = await _stocktakeService.SubmitLocationAsync(id, locationId, null);
            TempData[submitResult.Success ? "SuccessMessage" : "ErrorMessage"] = submitResult.Message;

            // After submit → back to session details so manager can see progress
            if (submitResult.Success)
                return RedirectToPage("/Stocktake/Details", new { id });

            return RedirectToPage("/Stocktake/Count", new { id, locationId });
        }

        // POST: add unexpected item (found in bin but not in system stock)
        public async Task<IActionResult> OnPostAddUnexpectedAsync(long id, long locationId)
        {
            CheckRole();
            if (!IsStaff)
                return Deny(id, locationId, "Bạn không có quyền thêm hàng không mong đợi.");

            if (ProductId <= 0 || ProductLotId <= 0 || UnexpectedQty <= 0)
            {
                TempData["ErrorMessage"] = "Chọn sản phẩm/lô và nhập số lượng > 0.";
                return RedirectToPage("/Stocktake/Count", new { id, locationId });
            }

            var result = await _stocktakeService.AddUnexpectedItemAsync(id, new UnexpectedStocktakeItemModel
            {
                StorageLocationId = locationId,
                ProductId = ProductId,
                ProductLotId = ProductLotId,
                CountedQuantity = UnexpectedQty,
                Notes = UnexpectedNotes
            });

            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Stocktake/Count", new { id, locationId });
        }

        private IActionResult Deny(long id, long locationId, string message)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToPage("/Stocktake/Count", new { id, locationId });
        }

        private void CheckRole()
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? string.Empty;
            IsManager = roleCode == "SYSTEM_ADMIN" || roleCode == "WAREHOUSE_MANAGER" || roleCode.Contains("MANAGER") || roleCode.Contains("ADMIN");
            IsStaff = IsManager || roleCode == "WAREHOUSE_STAFF";
        }
    }
}
