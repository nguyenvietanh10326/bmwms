using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMWMS.Web.Pages.OutboundOrders
{
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,SALES_STAFF,PURCHASING_STAFF")]
    public class DetailsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DetailsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public OutboundOrderDetailDto Order { get; set; } = new();
        public List<SelectListItem> AssigneeOptions { get; set; } = new();

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
                        if (User.IsInRole("WAREHOUSE_STAFF") &&
                            (!long.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId) ||
                             Order.AssignedToUserId != userId))
                            return Forbid();
                        if ((User.IsInRole("SYSTEM_ADMIN") || User.IsInRole("WAREHOUSE_MANAGER")) && Order.Status == "DRAFT")
                            await LoadAssigneesAsync(client);
                        return Page();
                    }
                }

                ErrorMessage = "Không thể tìm thấy chi tiết phiếu xuất kho.";
                return RedirectToPage("./Index");
            }
            catch (Exception)
            {
                ErrorMessage = "Không thể kết nối để tải phiếu xuất kho. Vui lòng thử lại.";
                return RedirectToPage("./Index");
            }
        }

        // POST: Xử lý Hủy lệnh xuất kho ngay từ trang Chi tiết
        public async Task<IActionResult> OnPostApproveAsync(long id, long assignedToUserId)
        {
            var response = await _httpClientFactory.CreateClient("ApiClient")
                .PostAsJsonAsync($"api/OutboundOrders/{id}/approve", new { assignedToUserId });
            await SetResultMessageAsync(response, "Đã duyệt và phân công phiếu xuất.");
            return RedirectToPage("./Details", new { id });
        }

        public async Task<IActionResult> OnPostStartAsync(long id)
        {
            var response = await _httpClientFactory.CreateClient("ApiClient")
                .PostAsync($"api/OutboundOrders/{id}/start", null);
            await SetResultMessageAsync(response, "Đã bắt đầu tác nghiệp xuất hàng.");
            return response.IsSuccessStatusCode
                ? RedirectToPage("./Process", new { id })
                : RedirectToPage("./Details", new { id });
        }

        public async Task<IActionResult> OnPostCancelOrderAsync(long id, string reason)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                var response = await client.PostAsJsonAsync($"api/OutboundOrders/{id}/cancel", new { reason });

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
            catch (Exception)
            {
                ErrorMessage = "Không thể kết nối để hủy phiếu xuất kho. Vui lòng thử lại.";
            }

            return RedirectToPage("./Details", new { id });
        }

        public async Task<IActionResult> OnPostCloseSalesRemainderAsync(long id, string reason)
        {
            var response = await _httpClientFactory.CreateClient("ApiClient")
                .PostAsJsonAsync($"api/OutboundOrders/{id}/close-sales-remainder", new { reason });
            await SetResultMessageAsync(response, "Đã đóng phần nhu cầu còn lại của SO.");
            return RedirectToPage("./Details", new { id });
        }

        private async Task LoadAssigneesAsync(HttpClient client)
        {
            try
            {
                var users = await client.GetFromJsonAsync<List<UserOptionDto>>("api/OutboundOrders/staff") ?? new();
                AssigneeOptions = users.Select(user => new SelectListItem(user.FullName, user.UserId.ToString())).ToList();
                AssigneeOptions.Insert(0, new SelectListItem(
                    AssigneeOptions.Count == 0 ? "-- Không có nhân viên kho đang rảnh --" : "-- Chọn nhân viên kho --", ""));
            }
            catch
            {
                AssigneeOptions = new() { new SelectListItem("-- Không tải được danh sách nhân viên --", "") };
            }
        }

        private async Task SetResultMessageAsync(HttpResponseMessage response, string successMessage)
        {
            ApiMessageDto? result = null;
            try { result = await response.Content.ReadFromJsonAsync<ApiMessageDto>(); }
            catch { }
            if (response.IsSuccessStatusCode) SuccessMessage = result?.Message ?? successMessage;
            else ErrorMessage = result?.Message ?? "Không thể thực hiện thao tác. Vui lòng tải lại phiếu và thử lại.";
        }
    }

    #region DTOs for Details View
    public class OutboundOrderDetailDto
    {
        public long OutboundOrderId { get; set; }
        public string OutboundOrderNumber { get; set; } = string.Empty;
        public long? SalesOrderId { get; set; }
        public long? PurchaseOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public string SourceReference { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public long WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? ExpectedIssueDate { get; set; }
        public long? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
        public long CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }
        public long? ApprovedByUserId { get; set; }
        public string? ApprovedByUserName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? CompletionType { get; set; }
        public string? CompletionReason { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? CancellationReason { get; set; }
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
        public string UnitOfMeasure { get; set; } = string.Empty;
        public byte QuantityScale { get; set; }
        public bool TrackLot { get; set; }
        public decimal RequestedQuantity { get; set; }
        public decimal IssuedQuantity { get; set; }
        public string? LotBin { get; set; }
        public string? Notes { get; set; }
        public List<OutboundPickedDetailViewDto> PickedDetails { get; set; } = new();
    }

    public class OutboundPickedDetailViewDto
    {
        public long OutboundOrderDetailId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LotNumber { get; set; } = string.Empty;
        public DateOnly FirstReceivedDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public decimal IssuedQuantity { get; set; }
        public string RecordedByUserName { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; }
    }

    public class ErrorResponseDetail
    {
        public string? Message { get; set; }
    }
    #endregion
}
