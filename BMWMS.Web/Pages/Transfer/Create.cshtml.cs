using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BMWMS.Web.Services;
using System.Text.Json;
using static BMWMS.Web.Services.TransferApiService;

namespace BMWMS.Web.Pages.Transfer
{
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public class CreateModel : PageModel
    {
        private readonly TransferApiService _transferSvc;

        public CreateModel(TransferApiService transferSvc)
        {
            _transferSvc = transferSvc;
        }

        // â”€â”€ Data for dropdowns â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€ Role check â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public bool IsManager { get; set; } = false;
        public string CurrentUserName { get; set; } = "";

        // â”€â”€ Result â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

        // â”€â”€ AJAX: Láº¥y Rack theo Zone â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public async Task<IActionResult> OnGetRacksByZoneAsync(long zoneId)
        {
            var racks = await _transferSvc.GetRacksAsync(1, zoneId > 0 ? zoneId : null);
            return new JsonResult(racks);
        }

        // â”€â”€ AJAX: Láº¥y Location theo Rack â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public async Task<IActionResult> OnGetLocationsByRackAsync(long rackId, long? zoneId = null)
        {
            var locs = await _transferSvc.GetLocationsAsync(1, zoneId, rackId > 0 ? rackId : null);
            return new JsonResult(locs);
        }

        // â”€â”€ AJAX: Láº¥y Location theo Zone (khÃ´ng cÃ³ Rack) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public async Task<IActionResult> OnGetLocationsByZoneAsync(long? zoneId)
        {
            var locs = await _transferSvc.GetLocationsAsync(1, zoneId, null);
            return new JsonResult(locs);
        }

        // â”€â”€ AJAX: Láº¥y tá»“n kho trong Ã´ nguá»“n â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public async Task<IActionResult> OnGetLocationInventoryAsync(long locationId)
        {
            var items = await _transferSvc.GetLocationInventoryAsync(locationId);
            return new JsonResult(items);
        }

        // â”€â”€ AJAX: Validate Ã´ Ä‘Ã­ch â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public async Task<IActionResult> OnGetValidateDestAsync(long destLocationId, long sourceLocationId, long productId)
        {
            var result = await _transferSvc.ValidateDestinationAsync(destLocationId, sourceLocationId, productId);
            return new JsonResult(result);
        }

        // â”€â”€ POST: Táº¡o lá»‡nh chuyá»ƒn kho â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public async Task<IActionResult> OnPostAsync(
            string itemsJson,
            string? action,
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
                TransferMessage = "Danh sÃ¡ch hÃ ng hÃ³a Ä‘iá»u chuyá»ƒn khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng.";
                TempData["ErrorMessage"] = TransferMessage;
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
                TransferMessage = $"Dá»¯ liá»‡u khÃ´ng há»£p lá»‡: {ex.Message}";
                TempData["ErrorMessage"] = TransferMessage;
                return await ReloadPageAsync();
            }

            if (items == null || !items.Any())
            {
                TransferSuccess = false;
                TransferMessage = "Vui lÃ²ng thÃªm Ã­t nháº¥t 1 máº·t hÃ ng cáº§n Ä‘iá»u chuyá»ƒn.";
                TempData["ErrorMessage"] = TransferMessage;
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
                if (action == "assign" && result.TransferOrderId.HasValue)
                {
                    var assignResult = await _transferSvc.ApproveOrderAsync(result.TransferOrderId.Value, request.AssignedToUserId, "Táº¡o vÃ  giao luÃ´n");
                    if (!assignResult.Success)
                    {
                        TransferSuccess = false;
                        TransferMessage = result.Message + " NhÆ°ng khÃ´ng thá»ƒ duyá»‡t vÃ  giao: " + assignResult.Message;
                        TempData["ErrorMessage"] = TransferMessage;
                        return await ReloadPageAsync();
                    }
                    TempData["SuccessMessage"] = assignResult.Message;
                }
                else
                {
                    TempData["SuccessMessage"] = result.Message;
                }
                
                return RedirectToPage("/Transfer/Details", new { id = result.TransferOrderId });
            }

            TransferSuccess = false;
            TransferMessage = result.Message;
            TempData["ErrorMessage"] = TransferMessage;
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
            CurrentUserName = HttpContext.Session.GetString("FullName") ?? HttpContext.Session.GetString("Username") ?? "NgÆ°á»i dÃ¹ng";
            IsManager = roleCode.Contains("ADMIN") || roleCode.Contains("MANAGER") || roleCode == "WAREHOUSE_MANAGER";
        }
    }
}

