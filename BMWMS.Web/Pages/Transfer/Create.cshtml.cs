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

        public List<ZoneOptionDto> Zones { get; set; } = new();
        public List<LocationOptionDto> Locations { get; set; } = new();
        public List<TransferInventoryItemDto> SourceInventory { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public long SelectedZoneId { get; set; } = 0;

        [BindProperty(SupportsGet = true)]
        public long SelectedSourceLocationId { get; set; } = 0;

        public bool? TransferSuccess { get; set; }
        public string TransferMessage { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            // Default WarehouseId = 1
            Zones     = await _transferSvc.GetZonesAsync(1);
            Locations = await _transferSvc.GetLocationsAsync(1, SelectedZoneId > 0 ? SelectedZoneId : null);

            if (SelectedSourceLocationId > 0)
            {
                SourceInventory = await _transferSvc.GetLocationInventoryAsync(SelectedSourceLocationId);
            }

            return Page();
        }

        public async Task<IActionResult> OnGetLocationsAsync(long? zoneId)
        {
            var locs = await _transferSvc.GetLocationsAsync(1, zoneId);
            return new JsonResult(locs);
        }

        public async Task<IActionResult> OnGetLocationInventoryAsync(long locationId)
        {
            var items = await _transferSvc.GetLocationInventoryAsync(locationId);
            return new JsonResult(items);
        }

        public async Task<IActionResult> OnGetValidateDestAsync(long destLocationId, long sourceLocationId, long productId)
        {
            var result = await _transferSvc.ValidateDestinationAsync(destLocationId, sourceLocationId, productId);
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostAsync(string itemsJson, string? notes)
        {
            if (string.IsNullOrWhiteSpace(itemsJson))
            {
                TransferSuccess = false;
                TransferMessage = "Danh sách sản phẩm điều chuyển không được để rỗng.";
                return await OnGetAsync();
            }

            List<CreateTransferItemDto>? items = null;
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
                TransferMessage = $"Dữ liệu danh sách sản phẩm không hợp lệ: {ex.Message}";
                return await OnGetAsync();
            }

            if (items == null || !items.Any())
            {
                TransferSuccess = false;
                TransferMessage = "Vui lòng chọn ít nhất 1 sản phẩm để điều chuyển.";
                return await OnGetAsync();
            }

            var request = new CreateTransferOrderDto
            {
                WarehouseId = 1, // Mặc định 1 kho
                Notes       = notes,
                Items       = items
            };

            var result = await _transferSvc.CreatePendingOrderAsync(request);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage("/Transfer/Index");
            }

            TransferSuccess = false;
            TransferMessage = result.Message;
            return await OnGetAsync();
        }
    }
}
