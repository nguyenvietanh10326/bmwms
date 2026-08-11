using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BMWMS.Web.Services;
using System.Text.Json;
using static BMWMS.Web.Services.TransferApiService;

namespace BMWMS.Web.Pages.Transfer
{
    public class CreateModel : PageModel
    {
        private readonly TransferApiService _transferSvc;

        public CreateModel(TransferApiService transferSvc)
        {
            _transferSvc = transferSvc;
        }

        // ── Data for dropdowns ──────────────────────────────────────────────
        public List<ZoneOptionDto> Zones { get; set; } = new();
        public List<StaffOptionDto> StaffUsers { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public long Id { get; set; }

        public bool IsEditMode => Id > 0;
        public TransferOrderDetailViewDto? EditingOrder { get; set; }
        public string InitialRowsJson { get; set; } = "[]";
        public long? SelectedAssignedToUserId { get; set; }
        public string? SelectedDueDate { get; set; }
        public string? ExistingNotes { get; set; }

        // ── Role check ──────────────────────────────────────────────────────
        public bool IsManager { get; set; } = false;
        public string CurrentUserName { get; set; } = "";

        // ── Result ──────────────────────────────────────────────────────────
        public bool? TransferSuccess { get; set; }
        public string TransferMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            LoadUserInfo();

            Zones      = await _transferSvc.GetZonesAsync(1);
            StaffUsers = await _transferSvc.GetStaffUsersAsync();

            if (Id > 0)
            {
                EditingOrder = await _transferSvc.GetOrderByIdAsync(Id);
                if (EditingOrder == null) return NotFound();
                if (!EditingOrder.CanEdit)
                {
                    TempData["ErrorMessage"] = "Chi co the sua phieu truoc khi phe duyet.";
                    return RedirectToPage("/Transfer/Details", new { id = Id });
                }

                SelectedAssignedToUserId = EditingOrder.AssignedToUserId;
                SelectedDueDate = EditingOrder.DueDate?.ToString("yyyy-MM-dd");
                ExistingNotes = EditingOrder.Notes;
                InitialRowsJson = BuildInitialRowsJson(EditingOrder);
            }

            return Page();
        }

        // ── AJAX: Lấy Rack theo Zone ─────────────────────────────────────────
        public async Task<IActionResult> OnGetRacksByZoneAsync(long zoneId)
        {
            var racks = await _transferSvc.GetRacksAsync(1, zoneId > 0 ? zoneId : null);
            return new JsonResult(racks);
        }

        // ── AJAX: Lấy Location theo Rack ──────────────────────────────────────
        public async Task<IActionResult> OnGetLocationsByRackAsync(long rackId, long? zoneId = null)
        {
            var locs = await _transferSvc.GetLocationsAsync(1, zoneId, rackId > 0 ? rackId : null);
            return new JsonResult(locs);
        }

        // ── AJAX: Lấy Location theo Zone (không có Rack) ──────────────────────
        public async Task<IActionResult> OnGetLocationsByZoneAsync(long? zoneId)
        {
            var locs = await _transferSvc.GetLocationsAsync(1, zoneId, null);
            return new JsonResult(locs);
        }

        // ── AJAX: Lấy tồn kho trong ô nguồn ──────────────────────────────────
        public async Task<IActionResult> OnGetLocationInventoryAsync(long locationId)
        {
            var items = await _transferSvc.GetLocationInventoryAsync(locationId);
            return new JsonResult(items);
        }

        // ── AJAX: Validate ô đích ────────────────────────────────────────────
        public async Task<IActionResult> OnGetValidateDestAsync(long destLocationId, long sourceLocationId, long productId)
        {
            var result = await _transferSvc.ValidateDestinationAsync(destLocationId, sourceLocationId, productId);
            return new JsonResult(result);
        }

        // ── POST: Tạo lệnh chuyển kho ─────────────────────────────────────────
        public async Task<IActionResult> OnPostAsync(
            string itemsJson,
            long? transferOrderId,
            long? assignedToUserId,
            string? dueDate,
            string? notes)
        {
            LoadUserInfo();
            Id = transferOrderId ?? 0;
            SelectedAssignedToUserId = assignedToUserId;
            SelectedDueDate = dueDate;
            ExistingNotes = notes;

            if (string.IsNullOrWhiteSpace(itemsJson))
            {
                TransferSuccess = false;
                TransferMessage = "Danh sách hàng hóa điều chuyển không được để trống.";
                return await ReloadPageAsync();
            }

            List<CreateTransferItemDto>? items;
            try
            {
                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                };
                items = JsonSerializer.Deserialize<List<CreateTransferItemDto>>(itemsJson, opts);
            }
            catch (Exception ex)
            {
                TransferSuccess = false;
                TransferMessage = $"Dữ liệu không hợp lệ: {ex.Message}";
                return await ReloadPageAsync();
            }

            if (items == null || !items.Any())
            {
                TransferSuccess = false;
                TransferMessage = "Vui lòng thêm ít nhất 1 mặt hàng cần điều chuyển.";
                return await ReloadPageAsync();
            }

            var request = new CreateTransferOrderDto
            {
                WarehouseId      = 1,
                AssignedToUserId = assignedToUserId > 0 ? assignedToUserId : null,
                DueDate          = dueDate,
                Notes            = notes,
                Items            = items
            };

            var result = Id > 0
                ? await _transferSvc.UpdateDraftOrderAsync(Id, new UpdateTransferOrderDto
                {
                    TransferOrderId = Id,
                    WarehouseId = request.WarehouseId,
                    AssignedToUserId = request.AssignedToUserId,
                    DueDate = request.DueDate,
                    Notes = request.Notes,
                    Items = request.Items
                })
                : await _transferSvc.CreatePendingOrderAsync(request);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage("/Transfer/Details", new { id = result.TransferOrderId });
            }

            TransferSuccess = false;
            TransferMessage = result.Message;
            return await ReloadPageAsync();
        }

        private async Task<IActionResult> ReloadPageAsync()
        {
            Zones      = await _transferSvc.GetZonesAsync(1);
            StaffUsers = await _transferSvc.GetStaffUsersAsync();
            return Page();
        }

        private static string BuildInitialRowsJson(TransferOrderDetailViewDto order)
        {
            var rows = order.Details.Select(d => new
            {
                sourceZoneId = d.SourceZoneId,
                sourceRackId = d.SourceRackId,
                sourceLocationId = d.SourceLocationId,
                destZoneId = d.DestZoneId,
                destRackId = d.DestRackId,
                destLocationId = d.DestLocationId,
                productId = d.ProductId,
                productLotId = d.ProductLotId,
                quantity = d.RequestedQuantity,
                unitName = d.UnitName
            });

            return JsonSerializer.Serialize(rows);
        }

        private void LoadUserInfo()
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpper() ?? "";
            CurrentUserName = HttpContext.Session.GetString("FullName") ?? HttpContext.Session.GetString("Username") ?? "Người dùng";
            IsManager = roleCode.Contains("ADMIN") || roleCode.Contains("MANAGER") || roleCode == "WAREHOUSE_MANAGER";
        }
    }
}
