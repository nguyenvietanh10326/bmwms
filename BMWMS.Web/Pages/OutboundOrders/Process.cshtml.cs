using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Text.Json;

namespace BMWMS.Web.Pages.OutboundOrders
{
    [Authorize(Roles = "WAREHOUSE_STAFF")]
    public class ProcessModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ProcessModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public OutboundOrderProcessDto Order { get; set; } = new();

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0) return RedirectToPage("./Index");

            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync($"api/OutboundOrders/{id}/process");

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = "Không thể tải thông tin xử lý đơn xuất kho.";
                return RedirectToPage("./Index");
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            Order = JsonSerializer.Deserialize<OutboundOrderProcessDto>(json, options) ?? new();

            return Page();
        }

        // Handler nhận AJAX Post từ Client để thực hiện Pick hàng
        public async Task<IActionResult> OnPostPickAsync([FromBody] PickItemRequestDto request)
        {
            if (request == null || request.OutboundOrderId <= 0 || request.PickQuantity <= 0)
            {
                return new JsonResult(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }

            var client = _httpClientFactory.CreateClient("ApiClient");
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/OutboundOrders/execute-pick", content);

            if (response.IsSuccessStatusCode)
            {
                return new JsonResult(new { success = true, message = "Đã lấy hàng thành công!" });
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            return new JsonResult(new { success = false, message = $"Lỗi từ server: {errorContent}" });
        }

        public async Task<IActionResult> OnPostPickBatchAsync([FromBody] List<PickItemRequestDto> requests)
        {
            if (requests == null || requests.Count == 0 || requests.Any(request =>
                    request.OutboundOrderId <= 0 || request.OutboundOrderItemId <= 0 || request.PickQuantity <= 0))
                return new JsonResult(new { success = false, message = "Phải chọn ít nhất một dòng lấy hàng hợp lệ." });

            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.PostAsJsonAsync("api/OutboundOrders/execute-pick-batch", requests);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiMessageDto>();
                return new JsonResult(new { success = true, message = result?.Message ?? "Đã ghi nhận đợt xuất hàng." });
            }

            var error = await response.Content.ReadFromJsonAsync<ApiMessageDto>();
            return new JsonResult(new { success = false, message = error?.Message ?? "Không thể ghi nhận đợt xuất hàng." });
        }

        public async Task<IActionResult> OnPostCompleteEarlyAsync(int id, string reason)
        {
            var response = await _httpClientFactory.CreateClient("ApiClient")
                .PostAsync($"api/OutboundOrders/{id}/complete-early?reason={Uri.EscapeDataString(reason ?? string.Empty)}", null);
            var result = await response.Content.ReadFromJsonAsync<ApiMessageDto>();
            if (response.IsSuccessStatusCode)
            {
                SuccessMessage = result?.Message ?? "Đã kết thúc đợt giao hàng.";
                return RedirectToPage("./Details", new { id });
            }

            ErrorMessage = result?.Message ?? "Không thể kết thúc đợt giao hàng.";
            return RedirectToPage(new { id });
        }
    }

    #region DTOs matching API
    public class OutboundOrderProcessDto
    {
        public int OutboundOrderId { get; set; }
        public string OutboundOrderNumber { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string PurchaseOrderNumber { get; set; } = string.Empty;
        public string SourceReference { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public long? AssignedToUserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime? ExpectedIssueDate { get; set; }
        public List<OutboundOrderItemProcessDto> Items { get; set; } = new();
    }

    public class OutboundOrderItemProcessDto
    {
        public int OutboundOrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public byte QuantityScale { get; set; }
        public bool TrackLot { get; set; }
        public decimal RequestedQuantity { get; set; }
        public decimal IssuedQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }
        public List<PickedDetailDto> PickedDetails { get; set; } = new();
        public List<AvailableLocationDto> AvailableLocations { get; set; } = new();
    }

    public class PickedDetailDto
    {
        public int StorageLocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public int ProductLotId { get; set; }
        public string LotNumber { get; set; } = string.Empty;
        public DateTime? FirstReceivedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal IssuedQuantity { get; set; }
    }

    public class AvailableLocationDto
    {
        public int StorageLocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationPath { get; set; } = string.Empty;
        public int ProductLotId { get; set; }
        public string LotNumber { get; set; } = string.Empty;
        public DateTime? FirstReceivedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? InventoryReservationId { get; set; }
        public decimal AvailableQuantity { get; set; }
    }

    public class PickItemRequestDto
    {
        public int OutboundOrderId { get; set; }
        public int OutboundOrderItemId { get; set; }
        public int StorageLocationId { get; set; }
        public int ProductLotId { get; set; }
        public int? InventoryReservationId { get; set; }
        public decimal PickQuantity { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
    public class ApiMessageDto { public string Message { get; set; } = string.Empty; }
    #endregion
}
