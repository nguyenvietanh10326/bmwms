using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BMWMS.Web.Services;
using static BMWMS.Web.Services.TransferApiService;

namespace BMWMS.Web.Pages.Transfer
{
    public class DetailsModel : PageModel
    {
        private readonly TransferApiService _transferSvc;

        public DetailsModel(TransferApiService transferSvc)
        {
            _transferSvc = transferSvc;
        }

        public TransferOrderDetailViewDto? Order { get; set; }

        [BindProperty(SupportsGet = true)]
        public long Id { get; set; }

        public bool IsManager { get; set; } = false;
        public bool IsStaff { get; set; } = false;
        public string CurrentRole { get; set; } = string.Empty;
        public long CurrentUserId { get; set; } = 0;

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            if (id <= 0) return RedirectToPage("/Transfer/Index");
            Id = id;
            Order = await _transferSvc.GetOrderByIdAsync(id);
            if (Order == null) return NotFound();
            CheckUserRole();
            return Page();
        }

        // ── Manager Approve ──────────────────────────────────────────────────
        public async Task<IActionResult> OnPostApproveAsync(long id, long? assignedToUserId, string? notes)
        {
            CheckUserRole();
            if (!IsManager)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền phê duyệt phiếu này.";
                return RedirectToPage("/Transfer/Details", new { id });
            }
            var result = await _transferSvc.ApproveOrderAsync(id, assignedToUserId, notes);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Transfer/Details", new { id });
        }

        // ── Manager Reject ───────────────────────────────────────────────────
        public async Task<IActionResult> OnPostRejectAsync(long id, string? notes)
        {
            CheckUserRole();
            if (!IsManager)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền từ chối phiếu này.";
                return RedirectToPage("/Transfer/Details", new { id });
            }
            var result = await _transferSvc.RejectOrderAsync(id, notes);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Transfer/Details", new { id });
        }

        // ── Staff Confirm Transfer ────────────────────────────────────────────
        public async Task<IActionResult> OnPostConfirmAsync(long id, string? notes)
        {
            CheckUserRole();
            // Both staff and manager can confirm
            var result = await _transferSvc.ConfirmTransferAsync(id, notes);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Transfer/Details", new { id });
        }

        public async Task<IActionResult> OnPostIssueAsync(long id, string? notes)
        {
            CheckUserRole();
            if (!IsStaff)
            {
                TempData["ErrorMessage"] = "Ban khong co quyen xac nhan xuat phieu nay.";
                return RedirectToPage("/Transfer/Details", new { id });
            }

            var result = await _transferSvc.ConfirmIssueAsync(id, notes);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Transfer/Details", new { id });
        }

        public async Task<IActionResult> OnPostReceiveAsync(long id, string? notes)
        {
            CheckUserRole();
            if (!IsStaff)
            {
                TempData["ErrorMessage"] = "Ban khong co quyen xac nhan nhap phieu nay.";
                return RedirectToPage("/Transfer/Details", new { id });
            }

            var result = await _transferSvc.ConfirmReceiptAsync(id, notes);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Transfer/Details", new { id });
        }

        private void CheckUserRole()
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpper() ?? "";
            var roleName = HttpContext.Session.GetString("RoleName") ?? "";
            if (long.TryParse(HttpContext.Session.GetString("UserId"), out var parsedUserId))
                CurrentUserId = parsedUserId;

            CurrentRole = !string.IsNullOrEmpty(roleName) ? roleName : (!string.IsNullOrEmpty(roleCode) ? roleCode : "User");
            IsManager   = roleCode.Contains("ADMIN") || roleCode.Contains("MANAGER") || roleCode == "WAREHOUSE_MANAGER";
            IsStaff     = roleCode == "WAREHOUSE_STAFF" || IsManager; // Manager can also act as staff
        }
    }
}
