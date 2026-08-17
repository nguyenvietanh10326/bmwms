using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BMWMS.Web.Pages.SaleOrder
{
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
            var response = await client.GetAsync($"api/SalesOrders/{id}");

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<SalesOrderDto>>(options);

                if (apiResult != null && apiResult.Success)
                {
                    SalesOrder = apiResult.Data;
                    return Page();
                }
            }

            return NotFound();
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
