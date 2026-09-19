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

        [BindProperty(SupportsGet = true)]
        public long? Id { get; set; }

        public bool IsEditMode => Id.HasValue && Id.Value > 0;
        public TransferOrderDetailViewDto? EditingOrder { get; set; }
        public List<BMWMS.Web.Models.WarehouseModel> Warehouses { get; set; } = new();

        [TempData] public string? ErrorMessage { get; set; }
        [TempData] public bool TransferSuccess { get; set; }
        [TempData] public string? TransferMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (!IsWarehouseStaff()) return RedirectToPage("/Transfer/Index");

            Warehouses = await _warehouseSvc.GetWarehousesAsync();
            if (IsEditMode)
            {
                EditingOrder = await _transferSvc.GetOrderByIdAsync(Id.Value);
                if (EditingOrder == null || EditingOrder.Status != "DRAFT")
                {
                    ErrorMessage = "Phiếu không tồn tại hoặc không ở trạng thái có thể sửa.";
                    return RedirectToPage("/Transfer/Index");
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnGetZonesAsync(long? productId)
        {
            return new JsonResult(await _transferSvc.GetZonesAsync(1, productId));
        }

        public async Task<IActionResult> OnGetRacksAsync(long? zoneId, long? productId)
        {
            return new JsonResult(await _transferSvc.GetRacksAsync(1, zoneId, productId));
        }

        public async Task<IActionResult> OnGetLocationsAsync(long? zoneId, long? rackId, long? productId)
        {
            return new JsonResult(await _transferSvc.GetLocationsAsync(1, zoneId, rackId, productId));
        }

        public async Task<IActionResult> OnGetLocationInventoryAsync(long locationId)
        {
            if (locationId <= 0) return new JsonResult(new List<TransferInventoryItemDto>());
            return new JsonResult(await _transferSvc.GetLocationInventoryAsync(locationId));
        }

        public async Task<IActionResult> OnGetValidateDestinationAsync(long locationId, long productId, decimal quantity)
        {
            if (locationId <= 0 || productId <= 0 || quantity <= 0)
                return new JsonResult(new BinCapacityCheckDto { IsValid = false, Message = "Du lieu kiem tra khong hop le." });
            return new JsonResult(await _transferSvc.ValidateDestinationAsync(locationId, productId, quantity));
        }

        public async Task<IActionResult> OnPostAsync(
            string action,
            DateOnly? dueDate,
            string? notes,
            List<long> productIds,
            List<long> productLotIds,
            List<long> sourceLocationIds,
            List<long> destLocationIds,
            List<decimal> quantities)
        {
            if (!IsWarehouseStaff()) return RedirectToPage("/Transfer/Index");

            if (productIds == null || productIds.Count == 0)
            {
                ErrorMessage = "Vui lòng thêm ít nhất 1 sản phẩm.";
                return RedirectToPage("/Transfer/Create", new { id = Id });
            }

            if (productLotIds.Count != productIds.Count ||
                sourceLocationIds.Count != productIds.Count ||
                destLocationIds.Count != productIds.Count ||
                quantities.Count != productIds.Count)
            {
                ErrorMessage = "Dữ liệu chi tiết phiếu không hợp lệ. Vui lòng kiểm tra lại các dòng hàng.";
                return RedirectToPage("/Transfer/Create", new { id = Id });
            }

            var request = new UpdateTransferOrderDto
            {
                WarehouseId = 1,
                DueDate = dueDate,
                Notes = notes,
                TransferOrderId = Id ?? 0
            };

            for (int i = 0; i < productIds.Count; i++)
            {
                request.Items.Add(new CreateTransferItemDto
                {
                    ProductId = productIds[i],
                    ProductLotId = productLotIds[i],
                    SourceLocationId = sourceLocationIds[i],
                    DestLocationId = destLocationIds[i],
                    Quantity = quantities[i]
                });
            }

            var result = IsEditMode
                ? await _transferSvc.UpdateDraftOrderAsync(Id.Value, request)
                : await _transferSvc.CreatePendingOrderAsync(request);

            if (result.Success)
            {
                TransferSuccess = true;
                TransferMessage = result.Message;
                return RedirectToPage("/Transfer/Details", new { id = result.TransferOrderId ?? Id });
            }
            else
            {
                ErrorMessage = result.Message;
                return RedirectToPage("/Transfer/Create", new { id = Id });
            }
        }

        private bool IsWarehouseStaff()
        {
            var roleCode = HttpContext.Session.GetString("RoleCode")?.ToUpperInvariant() ?? "";
            return roleCode == "WAREHOUSE_STAFF";
        }
    }
}
