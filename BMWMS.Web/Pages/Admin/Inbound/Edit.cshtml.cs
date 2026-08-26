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
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,PURCHASING_STAFF")]
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

                if (order.Status is not ("DRAFT" or "READY"))
                {
                    TempData["Error"] = "Chỉ có thể sửa phiếu nhập ở trạng thái Nháp hoặc Sẵn sàng, trước khi kiểm nhận.";
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

        public async Task<IActionResult> OnPostAsync(long id, string actionType)
        {
            EditOrder.IsSubmit = string.Equals(actionType, "submit", StringComparison.OrdinalIgnoreCase);
            if (EditOrder.IsSubmit && !EditOrder.AssignedToUserId.HasValue)
                ModelState.AddModelError("EditOrder.AssignedToUserId", "Vui lòng chọn nhân viên kho phụ trách trước khi gửi phiếu.");
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
            if (EditOrder.AssignedToUserId.HasValue &&
                warehouseUsers.All(user => user.UserId != EditOrder.AssignedToUserId.Value) &&
                Order != null && !string.IsNullOrWhiteSpace(Order.AssignedToUserName))
            {
                warehouseUsers.Add(new AvailableWarehouseStaffDto
                {
                    UserId = EditOrder.AssignedToUserId.Value,
                    FullName = $"{Order.AssignedToUserName} (đang phụ trách phiếu này)"
                });
            }
            Users = new SelectList(warehouseUsers, "UserId", "FullName", EditOrder.AssignedToUserId);
        }
    }
}
