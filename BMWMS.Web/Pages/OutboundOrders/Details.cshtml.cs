using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BMWMS.Web.Pages.OutboundOrders
{
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF")]
    public class DetailsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DetailsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public OutboundOrderDetailDto Order { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        // GET: Lấy thông tin chi tiết Lệnh Xuất Kho theo ID
        public async Task<IActionResult> OnGetAsync(long id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                var response = await client.GetAsync($"api/OutboundOrders/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<OutboundOrderDetailDto>();
                    if (result != null)
                    {
                        Order = result;
                        return Page();
                    }
                }

                ErrorMessage = "Không thể tìm thấy chi tiết lệnh xuất kho!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi kết nối API Backend: {ex.Message}";
                return RedirectToPage("./Index");
            }
        }

        // POST: Xử lý Hủy lệnh xuất kho ngay từ trang Chi tiết
        public async Task<IActionResult> OnPostCancelOrderAsync(long id)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                var response = await client.PostAsync($"api/OutboundOrders/{id}/cancel", null);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Hủy phiếu xuất kho thành công!";
                }
                else
                {
                    var errorData = await response.Content.ReadFromJsonAsync<ErrorResponseDetail>();
                    ErrorMessage = errorData?.Message ?? "Lỗi từ server khi thực hiện hủy phiếu!";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi kết nối API Backend: {ex.Message}";
            }

            return RedirectToPage("./Details", new { id });
        }
    }

    #region DTOs for Details View
    public class OutboundOrderDetailDto
    {
        public long OutboundOrderId { get; set; }
        public string OutboundOrderNumber { get; set; } = string.Empty;
        public long? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public string? CustomerName { get; set; }
        public long WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpectedIssueDate { get; set; }
        public long? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }

        public List<OutboundOrderItemDetailDto> Items { get; set; } = new();
    }

    public class OutboundOrderItemDetailDto
    {
        public long OutboundOrderItemId { get; set; }
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitName { get; set; } = "Cái";
        public decimal RequestedQuantity { get; set; }
        public decimal PickedQuantity { get; set; }
        public string? LotBin { get; set; }
        public string? Notes { get; set; }
    }

    public class ErrorResponseDetail
    {
        public string? Message { get; set; }
    }
    #endregion
}
