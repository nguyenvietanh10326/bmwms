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

        [TempData] public string? SuccessMessage { get; set; }
        [TempData] public string? ErrorMessage { get; set; }

        public bool IsManager { get; set; } = false;
        public bool IsStaff { get; set; } = false;

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
            var res = await _transferSvc.ApproveOrderAsync(id, notes);
            if (res.Success) SuccessMessage = "Đã phê duyệt phiếu thành công.";
            else ErrorMessage = res.Message;
            return RedirectToPage("/Transfer/Details", new { id });
        }

        public async Task<IActionResult> OnPostCancelAsync(long id, string? notes)
        {
            var res = await _transferSvc.CancelOrderAsync(id, notes);
            if (res.Success) SuccessMessage = "Đã hủy phiếu thành công.";
            else ErrorMessage = res.Message;
            return RedirectToPage("/Transfer/Details", new { id });
        }

        public async Task<IActionResult> OnPostConfirmAsync(
            long id,
            List<long> detailIds,
            List<long> destinationLocationIds,
            List<decimal> actualMovedQuantities,
            string? shortfallReason,
            string? destinationChangeReason,
            bool acknowledgeCapacityWarning,
            string? capacityWarningReason,
            string? notes)
        {
            if (detailIds == null || detailIds.Count == 0 || destinationLocationIds.Count != detailIds.Count ||
                actualMovedQuantities.Count != detailIds.Count)
            {
                ErrorMessage = "Dữ liệu xác nhận từng dòng hàng không đầy đủ. Vui lòng tải lại phiếu.";
                return RedirectToPage("/Transfer/Details", new { id });
            }
            var req = new ConfirmTransferDto {
                Notes = notes, ShortfallReason = shortfallReason, DestinationChangeReason = destinationChangeReason,
                AcknowledgeCapacityWarning = acknowledgeCapacityWarning, CapacityWarningReason = capacityWarningReason
            };
            for (int i = 0; i < detailIds.Count; i++)
            {
                req.Items.Add(new ConfirmTransferItemDto
                {
                    TransferOrderDetailId = detailIds[i],
                    DestinationLocationId = destinationLocationIds[i],
                    ActualMovedQuantity = actualMovedQuantities[i]
                });
            }

            var res = await _transferSvc.ConfirmTransferAsync(id, req);
            if (res.Success) SuccessMessage = "Đã xác nhận cất hàng và hoàn thành chuyển kho.";
            else ErrorMessage = res.Message;
            
            return RedirectToPage("/Transfer/Details", new { id });
        }

        private void CheckUserRole()
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpper() ?? "";
            IsManager = roleCode is "SYSTEM_ADMIN" or "WAREHOUSE_MANAGER";
            IsStaff = roleCode == "WAREHOUSE_STAFF";
        }
    }
}
