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
        public List<LocationOptionDto> DestinationLocations { get; set; } = new();

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
            if (Order.CanReceive)
            {
                DestinationLocations = (await _transferSvc.GetLocationsAsync(1))
                    .Where(location => location.IsPutawayAllowed)
                    .ToList();
            }
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

        public async Task<IActionResult> OnPostIssueAsync(
            long id,
            string? notes,
            List<long> detailIds,
            List<decimal> actualMovedQuantities,
            bool acknowledgeCapacityWarning,
            string? capacityWarningReason)
        {
            CheckUserRole();
            if (!IsStaff)
            {
                TempData["ErrorMessage"] = "Ban khong co quyen xac nhan xuat phieu nay.";
                return RedirectToPage("/Transfer/Details", new { id });
            }

            if (detailIds.Count != actualMovedQuantities.Count)
            {
                TempData["ErrorMessage"] = "Dữ liệu số lượng thực chuyển không hợp lệ.";
                return RedirectToPage("/Transfer/Details", new { id });
            }

            var request = new ConfirmTransferDto
            {
                Notes = notes,
                AcknowledgeCapacityWarning = acknowledgeCapacityWarning,
                CapacityWarningReason = capacityWarningReason,
                Items = detailIds.Select((detailId, index) => new ConfirmTransferItemDto
                {
                    TransferOrderDetailId = detailId,
                    ActualMovedQuantity = actualMovedQuantities[index]
                }).ToList()
            };
            var result = await _transferSvc.ConfirmIssueAsync(id, request);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Transfer/Details", new { id });
        }

        public async Task<IActionResult> OnPostReceiveAsync(
            long id,
            string? notes,
            List<long> receiptDetailIds,
            List<long> receiptDestinationIds,
            bool acknowledgeCapacityWarning,
            string? capacityWarningReason,
            string? destinationChangeReason)
        {
            CheckUserRole();
            if (!IsStaff)
            {
                TempData["ErrorMessage"] = "Ban khong co quyen xac nhan nhap phieu nay.";
                return RedirectToPage("/Transfer/Details", new { id });
            }

            if (receiptDetailIds.Count != receiptDestinationIds.Count)
            {
                TempData["ErrorMessage"] = "Dữ liệu vị trí đích không hợp lệ.";
                return RedirectToPage("/Transfer/Details", new { id });
            }

            var result = await _transferSvc.ConfirmReceiptAsync(id, new ConfirmTransferDto
            {
                Notes = notes,
                AcknowledgeCapacityWarning = acknowledgeCapacityWarning,
                CapacityWarningReason = capacityWarningReason,
                DestinationChangeReason = destinationChangeReason,
                Items = receiptDetailIds.Select((detailId, index) => new ConfirmTransferItemDto
                {
                    TransferOrderDetailId = detailId,
                    DestinationLocationId = receiptDestinationIds[index]
                }).ToList()
            });
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
            IsStaff     = roleCode == "WAREHOUSE_STAFF";
        }
    }
}
