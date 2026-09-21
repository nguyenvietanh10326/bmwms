using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

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

        public async Task<IActionResult> OnPostReviewCompletionAsync(long id, string? remainderAction, string? reason)
        {
            if (!User.IsInRole("SYSTEM_ADMIN") && !User.IsInRole("WAREHOUSE_MANAGER")) return Forbid();
            var response = await _httpClientFactory.CreateClient("ApiClient")
                .PostAsJsonAsync($"api/OutboundOrders/{id}/review-completion", new { remainderAction, reason });
            await SetResultMessageAsync(response, "Đã duyệt chốt đợt xuất.");
            return RedirectToPage("./Details", new { id });
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
        public bool CanCloseSalesRemainder { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool HasReferenceRemainder { get; set; }
        public long? CompletionReviewedByUserId { get; set; }
        public string? CompletionReviewedByUserName { get; set; }
        public List<OutboundRemainderViewDto> Remainders { get; set; } = new();
        public string? CancellationReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }

        public List<OutboundOrderItemDetailDto> Items { get; set; } = new();
    }

    public class OutboundRemainderViewDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public byte QuantityScale { get; set; }
        public decimal PlannedQuantity { get; set; }
        public decimal DeliveredQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }
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
