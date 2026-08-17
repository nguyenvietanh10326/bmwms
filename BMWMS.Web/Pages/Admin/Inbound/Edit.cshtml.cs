using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using BMWMS.Web.Services;
using BMWMS.Web.Models;
using Microsoft.AspNetCore.Authorization;

namespace BMWMS.Web.Pages.Admin.Inbound
{
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER")]
    public class EditModel : PageModel
    {
        private readonly InboundApiService _apiService;

        public EditModel(InboundApiService apiService)
        {
            _apiService = apiService;
        }

        [BindProperty]
        public UpdateInboundOrderDto EditOrder { get; set; } = new();

        public InboundOrderDetailDto Order { get; set; } = null!;
        public SelectList Users { get; set; } = null!;
        public string ErrorMessage { get; set; } = "";

        public async Task<IActionResult> OnGetAsync(long id)
        {
            try
            {
                var order = await _apiService.GetInboundOrderByIdAsync(id);
                if (order == null) return NotFound();

                if (order.Status != "DRAFT")
                {
                    TempData["Error"] = "Chỉ có thể sửa lệnh nhập kho ở trạng thái Nháp / Chờ xử lý.";
                    return RedirectToPage("./Details", new { id });
                }

                Order = order;
                EditOrder = new UpdateInboundOrderDto
                {
                    ExpectedReceiptDate = order.ExpectedReceiptDate,
                    Notes = "",
                    AssignedToUserId = order.AssignedToUserId,
                    Items = order.Items.Select(i => new UpdateInboundOrderItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        ExpectedQuantity = i.ExpectedQuantity
                    }).ToList()
                };

                await LoadDropdowns();
                
                return Page();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi tải dữ liệu: " + ex.Message;
                return RedirectToPage("./Index");
            }
        }

        public async Task<IActionResult> OnPostAsync(long id)
        {
            if (!ModelState.IsValid)
            {
                return await ReloadPage(id, "Vui lòng điền đầy đủ thông tin.");
            }

            try
            {
                await _apiService.UpdateInboundOrderAsync(id, EditOrder);
                TempData["Success"] = "Cập nhật lệnh nhập kho thành công.";
                return RedirectToPage("./Details", new { id });
            }
            catch (Exception ex)
            {
                return await ReloadPage(id, ex.Message);
            }
        }

        private async Task<IActionResult> ReloadPage(long id, string error)
        {
            ErrorMessage = error;
            var order = await _apiService.GetInboundOrderByIdAsync(id);
            Order = order;

            // Maintain items submitted
            for(int i = 0; i < EditOrder.Items.Count; i++)
            {
                var originalItem = Order.Items.FirstOrDefault(x => x.ProductId == EditOrder.Items[i].ProductId);
                if (originalItem != null)
                {
                    EditOrder.Items[i].ProductName = originalItem.ProductName;
                }
            }

            await LoadDropdowns();
            
            return Page();
        }

        private async Task LoadDropdowns()
        {
            var warehouseUsers = await _apiService.GetAvailableWarehouseStaffAsync();
            Users = new SelectList(warehouseUsers, "UserId", "FullName", EditOrder.AssignedToUserId);
        }
    }
}
