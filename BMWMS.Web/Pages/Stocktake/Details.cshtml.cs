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
        public List<StocktakeCountLineModel> Lines { get; set; } = new();

        [BindProperty]
        public List<long> ConfirmedEmptyLocationIds { get; set; } = new();

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
            if (!IsStaff || IsManager)
                return Deny(id, "Bạn không có quyền bắt đầu phiếu kiểm kho.");

            var session = await _stocktakeService.GetSessionByIdAsync(id);
            if (session != null && session.PlannedDate > DateOnly.FromDateTime(DateTime.Today))
                return Deny(id, $"Chưa đến ngày thực hiện kiểm kho ({session.PlannedDate:dd/MM/yyyy}). Người thực hiện chỉ có thể bắt đầu khi đến ngày dự kiến.");

            var result = await _stocktakeService.StartSessionAsync(id);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Stocktake/Details", new { id });
        }

        // BP-05: Warehouse Manager — Cancel session
        public async Task<IActionResult> OnPostCancelAsync(long id, string? notes)
        {
            CheckRole();
            if (!IsManager)
                return Deny(id, "Bạn không có quyền hủy phiếu kiểm kho.");

            var result = await _stocktakeService.CancelSessionAsync(id, notes);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Stocktake/Details", new { id });
        }

        public async Task<IActionResult> OnPostSaveCountsAsync(long id)
        {
            CheckRole();
            if (!IsStaff || IsManager)
                return Deny(id, "Bạn không có quyền nhập phiếu kiểm kho.");

            var session = await _stocktakeService.GetSessionByIdAsync(id);
            if (session != null && session.PlannedDate > DateOnly.FromDateTime(DateTime.Today))
                return Deny(id, $"Chưa đến ngày thực hiện kiểm kho ({session.PlannedDate:dd/MM/yyyy}).");

            var result = await _stocktakeService.SaveSessionCountsAsync(id, new SaveStocktakeSessionCountsModel
            {
                Lines = Lines,
                ConfirmedEmptyLocationIds = ConfirmedEmptyLocationIds
            });
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Stocktake/Details", new { id });
        }

        public async Task<IActionResult> OnPostSubmitAsync(long id)
        {
            CheckRole();
            if (!IsStaff || IsManager)
                return Deny(id, "Bạn không có quyền gửi kết quả phiếu kiểm kho.");

            var session = await _stocktakeService.GetSessionByIdAsync(id);
            if (session != null && session.PlannedDate > DateOnly.FromDateTime(DateTime.Today))
                return Deny(id, $"Chưa đến ngày thực hiện kiểm kho ({session.PlannedDate:dd/MM/yyyy}).");

            var saveResult = await _stocktakeService.SaveSessionCountsAsync(id, new SaveStocktakeSessionCountsModel
            {
                Lines = Lines,
                ConfirmedEmptyLocationIds = ConfirmedEmptyLocationIds
            });
            if (!saveResult.Success)
            {
                TempData["ErrorMessage"] = saveResult.Message;
                return RedirectToPage("/Stocktake/Details", new { id });
            }

            var submitResult = await _stocktakeService.SubmitSessionAsync(id);
            TempData[submitResult.Success ? "SuccessMessage" : "ErrorMessage"] = submitResult.Message;
            return RedirectToPage("/Stocktake/Details", new { id });
        }

        public async Task<IActionResult> OnPostRejectAsync(long id, string? reason)
        {
            CheckRole();
            if (!IsManager)
                return Deny(id, "Bạn không có quyền từ chối phiếu kiểm kho.");

            var result = await _stocktakeService.RejectSessionAsync(id, reason ?? string.Empty);
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
            IsStaff = roleCode == "WAREHOUSE_STAFF";
        }
    }
}
