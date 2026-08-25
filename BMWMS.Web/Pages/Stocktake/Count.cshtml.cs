using Microsoft.AspNetCore.Authorization;
using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace BMWMS.Web.Pages.Stocktake
{
    /// <summary>
    /// BP-05 â€” Warehouse Staff lane:
    ///   "Physically count each bin" â†’ "Enter actual quantity"
    /// Manager can also view; Submit locks the bin count.
    /// </summary>
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF")]
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

        // Helper method to parse lines from form
        private List<StocktakeCountLineModel> ParseLinesFromForm()
        {
            var lines = new List<StocktakeCountLineModel>();
            var form = Request.Form;
            
            Console.WriteLine($"[Count] ParseLinesFromForm - Form keys count: {form.Count}");
            
            // Get all line indices from form keys
            var indices = new HashSet<int>();
            foreach (var key in form.Keys)
            {
                if (key.StartsWith("Lines[") && key.Contains("].StocktakeItemId"))
                {
                    var start = key.IndexOf('[') + 1;
                    var end = key.IndexOf(']');
                    if (start > 0 && end > start)
                    {
                        var idxStr = key.Substring(start, end - start);
                        if (int.TryParse(idxStr, out var idx))
                        {
                            indices.Add(idx);
                        }
                    }
                }
            }
            
            Console.WriteLine($"[Count] Found {indices.Count} line indices");
            
            foreach (var idx in indices.OrderBy(x => x))
            {
                var prefix = $"Lines[{idx}]";
                var line = new StocktakeCountLineModel();
                
                // Parse StocktakeItemId
                var itemIdStr = form[$"{prefix}.StocktakeItemId"].FirstOrDefault();
                if (long.TryParse(itemIdStr, out var itemId))
                    line.StocktakeItemId = itemId;
                
                // Parse CountedQuantity
                var qtyStr = form[$"{prefix}.CountedQuantity"].FirstOrDefault();
                if (decimal.TryParse(qtyStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var qty))
                    line.CountedQuantity = qty;
                
                // Parse Notes
                line.Notes = form[$"{prefix}.Notes"].FirstOrDefault();
                
                // Parse other fields if needed
                var storageIdStr = form[$"{prefix}.StorageLocationId"].FirstOrDefault();
                if (long.TryParse(storageIdStr, out var storageId))
                    line.StorageLocationId = storageId;
                
                var productIdStr = form[$"{prefix}.ProductId"].FirstOrDefault();
                if (long.TryParse(productIdStr, out var productId))
                    line.ProductId = productId;
                
                var lotIdStr = form[$"{prefix}.ProductLotId"].FirstOrDefault();
                if (long.TryParse(lotIdStr, out var lotId))
                    line.ProductLotId = lotId;
                
                var bookQtyStr = form[$"{prefix}.BookQuantity"].FirstOrDefault();
                if (decimal.TryParse(bookQtyStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var bookQty))
                    line.BookQuantity = bookQty;
                
                if (line.StocktakeItemId > 0)
                {
                    lines.Add(line);
                    Console.WriteLine($"  Parsed line: ItemId={line.StocktakeItemId}, CountedQty={line.CountedQuantity}");
                }
            }
            
            return lines;
        }

        // POST: save draft counts (does not lock / submit)
        public async Task<IActionResult> OnPostSaveAsync(long id, long locationId, string? handler)
        {
            Console.WriteLine($"[Count] OnPostSaveAsync CALLED - id={id}, locationId={locationId}, handler={handler}");
            Console.WriteLine($"[Count] Request.Method={Request.Method}");
            Console.WriteLine($"[Count] Model binding - Lines count: {Lines?.Count ?? 0}");

            CheckRole();
            if (!IsStaff)
                return Deny(id, locationId, "Báº¡n khÃ´ng cÃ³ quyá»n nháº­p sá»‘ liá»‡u kiá»ƒm kho.");

            // Try model binding first, fallback to manual parsing
            if (Lines == null || Lines.Count == 0)
            {
                Lines = ParseLinesFromForm();
            }

            // Debug: Log received lines
            Console.WriteLine($"[Count] SaveCounts - SessionId: {id}, LocationId: {locationId}, Lines count: {Lines?.Count ?? 0}");
            if (Lines != null)
            {
                foreach (var line in Lines)
                {
                    Console.WriteLine($"  Line: StocktakeItemId={line.StocktakeItemId}, CountedQty={line.CountedQuantity}, Notes={line.Notes}");
                }
            }

            var result = await _stocktakeService.SaveCountsAsync(id, locationId, Lines ?? new());
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Stocktake/Count", new { id, locationId });
        }

        // POST: submit bin (lock count, trigger variance calculation on API side)
        public async Task<IActionResult> OnPostSubmitAsync(long id, long locationId, string? handler)
        {
            Console.WriteLine($"[Count] OnPostSubmitAsync CALLED - id={id}, locationId={locationId}, handler={handler}");

            CheckRole();
            if (!IsStaff)
                return Deny(id, locationId, "Báº¡n khÃ´ng cÃ³ quyá»n submit bin kiá»ƒm kho.");

            // Try model binding first, fallback to manual parsing
            if (Lines == null || Lines.Count == 0)
            {
                Lines = ParseLinesFromForm();
            }

            // Debug: Log received lines
            Console.WriteLine($"[Count] Submit - SessionId: {id}, LocationId: {locationId}, Lines count: {Lines?.Count ?? 0}");
            if (Lines != null)
            {
                foreach (var line in Lines)
                {
                    Console.WriteLine($"  Line: StocktakeItemId={line.StocktakeItemId}, CountedQty={line.CountedQuantity}, Notes={line.Notes}");
                }
            }

            // Validate that we have lines to submit
            if (Lines == null || Lines.Count == 0)
            {
                TempData["ErrorMessage"] = "KhÃ´ng cÃ³ dÃ²ng nÃ o Ä‘á»ƒ submit. Vui lÃ²ng nháº­p sá»‘ lÆ°á»£ng Ä‘áº¿m trÆ°á»›c.";
                return RedirectToPage("/Stocktake/Count", new { id, locationId });
            }

            // Save counts first, then submit
            var saveResult = await _stocktakeService.SaveCountsAsync(id, locationId, Lines);
            Console.WriteLine($"[Count] SaveCounts result: Success={saveResult.Success}, Message={saveResult.Message}");
            if (!saveResult.Success)
            {
                TempData["ErrorMessage"] = saveResult.Message;
                return RedirectToPage("/Stocktake/Count", new { id, locationId });
            }

            var submitResult = await _stocktakeService.SubmitLocationAsync(id, locationId, null);
            Console.WriteLine($"[Count] SubmitLocation result: Success={submitResult.Success}, Message={submitResult.Message}");

            // Always show message and redirect based on result
            if (submitResult.Success)
            {
                TempData["SuccessMessage"] = $"ÄÃ£ submit bin thÃ nh cÃ´ng. Äang chuyá»ƒn vá» trang chi tiáº¿t...";
                return RedirectToPage("/Stocktake/Details", new { id });
            }
            else
            {
                TempData["ErrorMessage"] = submitResult.Message;
                return RedirectToPage("/Stocktake/Count", new { id, locationId });
            }
        }

        // POST: add unexpected item (found in bin but not in system stock)
        public async Task<IActionResult> OnPostAddUnexpectedAsync(long id, long locationId)
        {
            CheckRole();
            if (!IsStaff)
                return Deny(id, locationId, "Báº¡n khÃ´ng cÃ³ quyá»n thÃªm hÃ ng khÃ´ng mong Ä‘á»£i.");

            if (ProductId <= 0 || ProductLotId <= 0 || UnexpectedQty <= 0)
            {
                TempData["ErrorMessage"] = "Chá»n sáº£n pháº©m/lÃ´ vÃ  nháº­p sá»‘ lÆ°á»£ng > 0.";
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

