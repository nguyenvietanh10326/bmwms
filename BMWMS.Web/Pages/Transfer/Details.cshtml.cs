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

        public bool IsManager { get; set; } = true;
        public string CurrentRole { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(long id)
        {
            if (id <= 0) return RedirectToPage("/Transfer/Index");

            Id = id;
            Order = await _transferSvc.GetOrderByIdAsync(id);
            if (Order == null) return NotFound();

            CheckUserRole();
            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(long id, string? notes)
        {
            CheckUserRole();

            if (!IsManager)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền phê duyệt phiếu này (Yêu cầu quyền Quản lý kho / ADMIN).";
                return RedirectToPage("/Transfer/Details", new { id });
            }

            var result = await _transferSvc.ApproveOrderAsync(id, notes);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToPage("/Transfer/Details", new { id });
        }

        public async Task<IActionResult> OnPostRejectAsync(long id, string? notes)
        {
            CheckUserRole();

            if (!IsManager)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền từ chối phiếu này (Yêu cầu quyền Quản lý kho / ADMIN).";
                return RedirectToPage("/Transfer/Details", new { id });
            }

            var result = await _transferSvc.RejectOrderAsync(id, notes);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToPage("/Transfer/Details", new { id });
        }

        private void CheckUserRole()
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpper();
            var roleName = HttpContext.Session.GetString("RoleName");
            CurrentRole = !string.IsNullOrEmpty(roleName) ? roleName : (!string.IsNullOrEmpty(roleCode) ? roleCode : "User");

            // Nếu roleCode là WH_STAFF hoặc STAFF (không phải ADMIN/WH_MANAGER/MANAGER), IsManager = false
            if (!string.IsNullOrEmpty(roleCode))
            {
                IsManager = roleCode.Contains("ADMIN") || roleCode.Contains("MANAGER") || roleCode == "WH_MANAGER";
            }
            else
            {
                IsManager = true; // Default allow if session not strict
            }
        }
    }
}
