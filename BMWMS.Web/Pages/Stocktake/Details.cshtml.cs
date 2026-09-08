using BMWMS.Web.Models;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.Stocktake
{
    public class DetailsModel : PageModel
    {
        private readonly IStocktakeApiService _stocktakeService;

        public DetailsModel(IStocktakeApiService stocktakeService)
        {
            _stocktakeService = stocktakeService;
        }

        public StocktakeSessionDetailModel? SessionDetail { get; set; }
        public bool IsManager { get; set; }
        public bool IsStaff { get; set; }

        [BindProperty(SupportsGet = true)]
        public long Id { get; set; }

        [BindProperty]
        public List<StocktakeResolutionModel> Resolutions { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            if (id <= 0)
                return RedirectToPage("/Stocktake/Index");

            Id = id;
            CheckRole();
            SessionDetail = await _stocktakeService.GetSessionByIdAsync(id);
            if (SessionDetail == null)
                return NotFound();

            return Page();
        }

        // BP-05: Warehouse Manager — Start session → BMWMS snapshots system quantity
        public async Task<IActionResult> OnPostStartAsync(long id)
        {
            CheckRole();
            if (!IsManager)
                return Deny(id, "Bạn không có quyền bắt đầu đợt kiểm kho.");

            var result = await _stocktakeService.StartSessionAsync(id);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Stocktake/Details", new { id });
        }

        // BP-05: Warehouse Manager — Cancel session
        public async Task<IActionResult> OnPostCancelAsync(long id, string? notes)
        {
            CheckRole();
            if (!IsManager)
                return Deny(id, "Bạn không có quyền huỷ đợt kiểm kho.");

            var result = await _stocktakeService.CancelSessionAsync(id, notes);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Stocktake/Details", new { id });
        }

        // BP-05: Warehouse Manager — Request recount / investigation per bin
        public async Task<IActionResult> OnPostRequestRecountAsync(long id, long locationId)
        {
            CheckRole();
            if (!IsManager)
                return Deny(id, "Bạn không có quyền yêu cầu đếm lại.");

            var result = await _stocktakeService.RequestRecountAsync(id, locationId);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Stocktake/Details", new { id });
        }

        // BP-05: Warehouse Manager — Save resolutions for variance lines (PENDING_APPROVAL state)
        public async Task<IActionResult> OnPostResolutionsAsync(long id)
        {
            CheckRole();
            if (!IsManager)
                return Deny(id, "Bạn không có quyền review chênh lệch.");

            var cleaned = Resolutions
                .Where(r => r.StocktakeItemId > 0 && !string.IsNullOrWhiteSpace(r.Resolution))
                .ToList();

            var result = await _stocktakeService.ApplyResolutionsAsync(id, cleaned);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Stocktake/Details", new { id });
        }

        // BP-05: Warehouse Manager — Submit for approval (COUNTED → PENDING_APPROVAL)
        public async Task<IActionResult> OnPostSubmitForReviewAsync(long id)
        {
            CheckRole();
            if (!IsManager)
                return Deny(id, "Bạn không có quyền gửi phê duyệt.");

            var result = await _stocktakeService.SubmitForReviewAsync(id);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Stocktake/Details", new { id });
        }

        // BP-05: Warehouse Manager — Approve stock adjustment → BMWMS posts adjustment + audit
        public async Task<IActionResult> OnPostApproveAsync(
            long id,
            string? notes,
            bool acknowledgeCapacityWarning,
            string? capacityWarningReason)
        {
            CheckRole();
            if (!IsManager)
                return Deny(id, "Bạn không có quyền phê duyệt điều chỉnh tồn.");

            var result = await _stocktakeService.ApproveSessionAsync(id, new StocktakeNoteModel
            {
                Notes = notes,
                AcknowledgeCapacityWarning = acknowledgeCapacityWarning,
                CapacityWarningReason = capacityWarningReason
            });
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Stocktake/Details", new { id });
        }

        private IActionResult Deny(long id, string message)
        {
            TempData["ErrorMessage"] = message;
            return RedirectToPage("/Stocktake/Details", new { id });
        }

        private void CheckRole()
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? string.Empty;
            IsManager = roleCode == "SYSTEM_ADMIN" || roleCode == "WAREHOUSE_MANAGER" || roleCode.Contains("MANAGER") || roleCode.Contains("ADMIN");
            IsStaff = IsManager || roleCode == "WAREHOUSE_STAFF";
        }
    }
}
