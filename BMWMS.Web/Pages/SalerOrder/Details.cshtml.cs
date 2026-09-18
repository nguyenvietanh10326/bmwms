using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BMWMS.Web.Pages.SaleOrder
{
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF,ACCOUNTANT,DIRECTOR")]
    public class DetailsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public DetailsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public SalesOrderDto? SalesOrder { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            // Gọi API backend lấy dữ liệu JSON
            try
            {
                var response = await client.GetAsync($"api/SalesOrders/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>(options);
                    if (apiResult?.Success == true && apiResult.Data != null)
                    {
                        SalesOrder = apiResult.Data;
                        return Page();
                    }
                }
                return NotFound();
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                TempData["ErrorMessage"] = "Không thể tải SO do lỗi kết nối. Vui lòng tải lại và kiểm tra trạng thái trước khi thao tác.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmAsync(long id, string rowVersion)
        {
            if (!User.IsInRole("WAREHOUSE_MANAGER") && !User.IsInRole("SYSTEM_ADMIN"))
                return Forbid();
            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.PostAsync($"api/SalesOrders/{id}/confirm?rowVersion={Uri.EscapeDataString(rowVersion ?? "")}", null);
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Đã xác nhận đơn hàng thành công.";
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                TempData["ErrorMessage"] = "Không thể xác nhận đơn hàng: " + error;
            }
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostCancelAsync(long id, string reason) => await ChangeStateAsync(id, "cancel", reason);
        public async Task<IActionResult> OnPostRejectAsync(long id, string reason, string? rowVersion) => await ChangeStateAsync(id, "reject", reason, rowVersion);
        public async Task<IActionResult> OnPostReviewOutboundCompletionAsync(int id, long outboundOrderId, string? remainderAction, string? reason)
        {
            if (!User.IsInRole("WAREHOUSE_MANAGER") && !User.IsInRole("SYSTEM_ADMIN")) return Forbid();
            try
            {
                var loaded = await OnGetAsync(id);
                if (loaded is not PageResult) return loaded;
                if (SalesOrder == null) return RedirectToPage(new { id });
                if (!SalesOrder.OutboundBatches.Any(o => o.OutboundOrderId == outboundOrderId && o.Status == "PENDING_APPROVAL"))
                {
                    TempData["ErrorMessage"] = "Đợt xuất không thuộc SO này hoặc đã được duyệt. Vui lòng tải lại phiếu.";
                    return RedirectToPage(new { id });
                }
                var response = await _httpClientFactory.CreateClient("ApiClient")
                    .PostAsJsonAsync($"api/OutboundOrders/{outboundOrderId}/review-completion", new { remainderAction, reason });
                TempData[response.IsSuccessStatusCode ? "SuccessMessage" : "ErrorMessage"] = response.IsSuccessStatusCode
                    ? (SalesOrder.Items.Any(x => x.FulfilledQuantity < x.OrderedQuantity) && remainderAction?.Trim().ToUpperInvariant() == "CONTINUE"
                        ? "Đã duyệt đợt xuất. SO đã xuất một phần; nhân viên kho có thể tạo đợt tiếp theo cho lượng còn lại."
                        : "Đã duyệt chốt đợt xuất. SO đã xuất và không giao tiếp phần còn lại.")
                    : await BMWMS.Web.Services.ApiErrorReader.ReadAsync(response);
                return RedirectToPage(new { id });
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                TempData["ErrorMessage"] = "Không xác định được kết quả duyệt do lỗi kết nối. Vui lòng tải lại SO và kiểm tra trạng thái trước khi thử lại.";
                return RedirectToPage(new { id });
            }
        }
        private async Task<IActionResult> ChangeStateAsync(long id, string action, string reason, string? rowVersion = null)
        {
            if (action == "reject" && !User.IsInRole("WAREHOUSE_MANAGER") && !User.IsInRole("SYSTEM_ADMIN")) return Forbid();
            if (action == "cancel" && !User.IsInRole("SALES_STAFF") && !User.IsInRole("WAREHOUSE_MANAGER") && !User.IsInRole("SYSTEM_ADMIN")) return Forbid();
            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.PostAsJsonAsync($"api/SalesOrders/{id}/{action}", new { reason, rowVersion });
            TempData[response.IsSuccessStatusCode ? "SuccessMessage" : "ErrorMessage"] =
                response.IsSuccessStatusCode ? "Đã cập nhật SO." : await BMWMS.Web.Services.ApiErrorReader.ReadAsync(response);
            return RedirectToPage(new { id });
        }
    }

    // --- CÁC CLASS DTO KHỚP CHÍNH XÁC VỚI RESPONSE JSON BẠN CUNG CẤP ---

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }

    public class SalesOrderDto
    {
        public long CreatedByUserId { get; set; }
        public string RowVersion { get; set; } = "";
        public bool CanEdit { get; set; }
        public int SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? ExpectedIssueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public string AllocationStrategy { get; set; } = "FIFO";
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ConfirmedByName { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public List<SalesOrderItemDto> Items { get; set; } = new();
        public List<BMWMS.Web.Models.Inventory.SalesOrderOutboundBatchDto> OutboundBatches { get; set; } = new();
    }

    public class SalesOrderItemDto
    {
        public int SalesOrderDetailId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public byte QuantityScale { get; set; }
        public bool TrackLot { get; set; }
        public decimal OrderedQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal FulfilledQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public string? Notes { get; set; }
        public string AllocationStrategy { get; set; } = "FIFO";
    }
}
