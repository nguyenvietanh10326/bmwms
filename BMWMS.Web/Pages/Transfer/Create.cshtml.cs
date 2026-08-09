using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BMWMS.Web.Services;
using static BMWMS.Web.Services.TransferApiService;

namespace BMWMS.Web.Pages.Transfer
{
    public class CreateModel : PageModel
    {
        private readonly TransferApiService _transferSvc;
        private readonly WarehouseApiService _warehouseSvc;

        public CreateModel(TransferApiService transferSvc, WarehouseApiService warehouseSvc)
        {
            _transferSvc = transferSvc;
            _warehouseSvc = warehouseSvc;
        }

        public List<BMWMS.Web.Models.WarehouseModel> Warehouses { get; set; } = new();
        public List<LocationOptionDto> Locations { get; set; } = new();
        public List<TransferInventoryItemDto> SourceInventory { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public long SelectedWarehouseId { get; set; } = 0;

        [BindProperty(SupportsGet = true)]
        public long SelectedSourceLocationId { get; set; } = 0;

        public bool? TransferSuccess { get; set; }
        public string TransferMessage { get; set; } = string.Empty;
        public string? TransferOrderNumber { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            Warehouses = await _warehouseSvc.GetWarehousesAsync();

            if (SelectedWarehouseId <= 0 && Warehouses.Any())
            {
                SelectedWarehouseId = Warehouses.First().WarehouseId;
            }

            if (SelectedWarehouseId > 0)
            {
                Locations = await _transferSvc.GetLocationsAsync(SelectedWarehouseId);
            }

            if (SelectedSourceLocationId > 0)
            {
                SourceInventory = await _transferSvc.GetLocationInventoryAsync(SelectedSourceLocationId);
            }

            return Page();
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

        public async Task<IActionResult> OnPostAsync(
            long warehouseId,
            long sourceLocationId,
            long destLocationId,
            long productId,
            long productLotId,
            decimal quantity,
            string? notes)
        {
            SelectedWarehouseId = warehouseId;
            SelectedSourceLocationId = sourceLocationId;

            var request = new CreateTransferOrderDto
            {
                WarehouseId      = warehouseId,
                SourceLocationId = sourceLocationId,
                DestLocationId   = destLocationId,
                ProductId        = productId,
                ProductLotId     = productLotId,
                Quantity         = quantity,
                Notes            = notes
            };

            var result = await _transferSvc.CreatePendingOrderAsync(request);
            TransferSuccess     = result.Success;
            TransferMessage     = result.Message;
            TransferOrderNumber = result.TransferOrderNumber;

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToPage("/Transfer/Index");
            }

            // Reload data if error
            Warehouses      = await _warehouseSvc.GetWarehousesAsync();
            Locations       = await _transferSvc.GetLocationsAsync(warehouseId);
            SourceInventory = await _transferSvc.GetLocationInventoryAsync(sourceLocationId);

            return Page();
        }
    }
}
