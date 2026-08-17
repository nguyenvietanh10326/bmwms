using BMWMS.Web.Models;
using BMWMS.Web.Models.Inventory;
using BMWMS.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace BMWMS.Web.Pages.PurchaseOrders
{
    // RBAC: SYSTEM_ADMIN, WAREHOUSE_MANAGER, PURCHASING_STAFF
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF")]
    public class CreateModel : PageModel
    {
        private readonly SupplierApiService _supplierApiService;
        private readonly PurchaseOrderApiService _poApiService;

        public CreateModel(SupplierApiService supplierApiService, PurchaseOrderApiService poApiService)
        {
            _supplierApiService = supplierApiService;
            _poApiService = poApiService;
        }

        [BindProperty]
        public PurchaseOrderCreateRequestModel PurchaseOrder { get; set; } = new();

        public List<SupplierLookupDto> Suppliers { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadSuppliersAsync();
            PurchaseOrder.OrderDate = System.DateOnly.FromDateTime(System.DateTime.Today);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSuppliersAsync();
                return Page();
            }

            // Remove any empty details if Javascript allowed them through
            PurchaseOrder.OrderDetails = PurchaseOrder.OrderDetails
                .Where(x => x.ProductId > 0 && x.OrderedQuantity > 0).ToList();

            var result = await _poApiService.CreatePurchaseOrderAsync(PurchaseOrder);
            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = $"Đã tạo lệnh mua hàng {result.PoNumber} thành công (Trạng thái: DRAFT).";
                return RedirectToPage("/PurchaseOrders/Index"); // Assuming an Index page will exist
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Lỗi không xác định");
            await LoadSuppliersAsync();
            return Page();
        }

        
        public async Task<IActionResult> OnGetSupplierProductsAsync(string supplierCode)
        {
            var detail = await _supplierApiService.GetSupplierDetailAsync(supplierCode);
            if (detail != null)
            {
                return new JsonResult(new { suppliedProducts = detail.SuppliedProducts });
            }
            return new BadRequestResult();
        }
private async Task LoadSuppliersAsync()
        {
            // Fetch all active suppliers for dropdown
            var list = await _supplierApiService.GetPagedListAsync(new SupplierFilterModel { PageSize = 1000 });
            if (list != null)
            {
                // We map to a basic LookupDto structure for the dropdown
                Suppliers = list.Items.Select(s => new SupplierLookupDto { SupplierId = s.SupplierId, SupplierName = s.SupplierName, SupplierCode = s.SupplierCode }).ToList();
            }
        }
    }

    public class SupplierLookupDto
    {
        public long SupplierId { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
    }
}
