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
            if (Order.CanConfirm)
            {
                DestinationLocations = (await _transferSvc.GetLocationsAsync(1))
                    .Where(location => location.IsPutawayAllowed)
                    .ToList();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostApproveAsync(long id, string? notes)
        {
            CheckUserRole();
            if (!IsManager)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền duyệt phiếu này.";
                return RedirectToPage("/Transfer/Details", new { id });
            }
            var result = await _transferSvc.ApproveOrderAsync(id, notes);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Transfer/Details", new { id });
        }

        public async Task<IActionResult> OnPostCancelAsync(long id, string? notes)
        {
            CheckUserRole();
            var result = await _transferSvc.CancelOrderAsync(id, notes);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToPage("/Transfer/Details", new { id });
        }

        public async Task<IActionResult> OnPostConfirmAsync(
            long id,
            string? notes,
            List<long> detailIds,
            List<decimal> actualMovedQuantities,
            List<long> destinationLocationIds,
            bool acknowledgeCapacityWarning,
            string? capacityWarningReason,
            string? destinationChangeReason,
            string? shortfallReason)
        {
            CheckUserRole();
            if (!IsStaff)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xác nhận phiếu này.";
                return RedirectToPage("/Transfer/Details", new { id });
            }

            if (detailIds.Count != actualMovedQuantities.Count || detailIds.Count != destinationLocationIds.Count)
            {
                TempData["ErrorMessage"] = "Dữ liệu xác nhận không hợp lệ.";
                return RedirectToPage("/Transfer/Details", new { id });
            }

            var request = new ConfirmTransferDto
            {
                Notes = notes,
                AcknowledgeCapacityWarning = acknowledgeCapacityWarning,
                CapacityWarningReason = capacityWarningReason,
                DestinationChangeReason = destinationChangeReason,
                ShortfallReason = shortfallReason,
                Items = detailIds.Select((detailId, index) => new ConfirmTransferItemDto
                {
                    TransferOrderDetailId = detailId,
                    ActualMovedQuantity = actualMovedQuantities[index],
                    DestinationLocationId = destinationLocationIds[index]
                }).ToList()
            };

            var result = await _transferSvc.ConfirmTransferAsync(id, request);
            if (!result.Success && result.Message.Contains("đã bị đầy"))
            {
                TempData["ErrorMessage"] = result.Message;
            }
            else
            {
                TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            }
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
