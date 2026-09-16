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
            Warehouses = await _warehouseSvc.GetWarehousesAsync();
            if (IsEditMode)
            {
                EditingOrder = await _transferSvc.GetOrderByIdAsync(Id.Value);
                if (EditingOrder == null || EditingOrder.Status != ""DRAFT"")
                {
                    ErrorMessage = ""Phiếu không tồn tại hoặc không ở trạng thái có thể s�a."";
                    return RedirectToPage(""/Transfer/Index"");
                }
            }
            return Page();
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
            if (productIds == null || productIds.Count == 0)
            {
                ErrorMessage = ""Vui lòng thêm ít nhất 1 sản phẩm."";
                return RedirectToPage(""/Transfer/Create"", new { id = Id });
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
                return RedirectToPage(""/Transfer/Index"");
            }
            else
            {
                ErrorMessage = result.Message;
                return RedirectToPage(""/Transfer/Create"", new { id = Id });
            }
        }
    }
}
