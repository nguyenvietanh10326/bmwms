using BMWMS.Web.Models.Inventory;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMWMS.Web.Pages.SalesOrders
{
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(IHttpClientFactory httpClientFactory, ILogger<CreateModel> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [BindProperty]
        public CreateUpdateSalesOrderDto SalesOrder { get; set; } = new()
        {
            OrderDate = DateOnly.FromDateTime(DateTime.Today)
        };

        public List<SelectListItem> CustomerOptions { get; set; } = new();
        public List<ProductDto> ProductList { get; set; } = new();

        public IEnumerable<ProductGroupOption> ProductGroups => ProductList
            .Where(p => p.ProductGroupId > 0)
            .GroupBy(p => new { p.ProductGroupId, p.ProductGroupName })
            .Select(g => new ProductGroupOption
            {
                ProductGroupId = g.Key.ProductGroupId,
                ProductGroupName = g.Key.ProductGroupName
            })
            .OrderBy(g => g.ProductGroupName);

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (!User.IsInRole("SALES_STAFF") && !User.IsInRole("SYSTEM_ADMIN")) return Forbid();
            await LoadFormDataAsync();
            if (id.HasValue)
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                var result = await client.GetFromJsonAsync<ApiWrapper<SalesOrderDetailDto>>($"api/SalesOrders/{id}");
                var order = result?.Data;
                if (order?.CanEdit != true) return BadRequest("SO đã có phiếu nhập/xuất hoặc không còn được sửa.");
                SalesOrder = new CreateUpdateSalesOrderDto
                {
                    SalesOrderId = order.SalesOrderId, CustomerId = order.CustomerId,
                    RowVersion = order.RowVersion,
                    OrderDate = order.OrderDate, ExpectedIssueDate = order.ExpectedIssueDate,
                    Notes = order.Notes, AllocationStrategy = order.AllocationStrategy,
                    Items = order.Items.Select(i => new CreateUpdateSalesOrderItemDto
                    { ProductId = i.ProductId, OrderedQuantity = i.OrderedQuantity, Notes = i.Notes }).ToList()
                };
                foreach (var line in order.Items)
                {
                    var product = ProductList.FirstOrDefault(p => p.ProductId == line.ProductId);
                    if (product != null) product.AvailableQuantity = product.Available + line.ReservedQuantity;
                }
            }
            return Page();
        }

        public async Task<IActionResult> OnPostSaveDraftAsync()
        {
            ValidateBusinessInput();

            if (!ModelState.IsValid)
            {
                ErrorMessage = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                    ?? "Vui lòng kiểm tra lại thông tin đơn bán hàng.";
                await LoadFormDataAsync();
                return Page();
            }

            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                var response = SalesOrder.SalesOrderId.HasValue
                    ? await client.PutAsJsonAsync($"api/SalesOrders/{SalesOrder.SalesOrderId}", SalesOrder)
                    : await client.PostAsJsonAsync("api/SalesOrders", SalesOrder);
                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage = await ReadApiErrorAsync(response, "Không thể tạo đơn bán hàng.");
                    await LoadFormDataAsync();
                    return Page();
                }

                if (SalesOrder.SalesOrderId.HasValue)
                {
                    TempData["SuccessMessage"] = "Đã cập nhật SO nháp và giữ tồn theo số mới.";
                    return RedirectToPage("./Details", new { id = SalesOrder.SalesOrderId });
                }
                var result = await response.Content
                    .ReadFromJsonAsync<ApiWrapper<SalesOrderDetailDto>>();
                var salesOrderId = result?.Data?.SalesOrderId ?? 0;

                if (salesOrderId <= 0)
                {
                    ErrorMessage = "Đơn đã được lưu nhưng không thể mở trang chi tiết.";
                    await LoadFormDataAsync();
                    return Page();
                }

                TempData["SuccessMessage"] = "Tạo đơn bán hàng nháp thành công.";
                return RedirectToPage("./Details", new { id = salesOrderId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot create sales order draft from the web form.");
                ErrorMessage = "Không thể kết nối đến hệ thống dữ liệu. Vui lòng thử lại sau.";
                await LoadFormDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCreateCustomerAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                using var reader = new StreamReader(Request.Body);
                var json = await reader.ReadToEndAsync();
                var input = JsonSerializer.Deserialize<CreateCustomerInput>(
                    json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (input == null)
                {
                    Response.StatusCode = StatusCodes.Status400BadRequest;
                    return new JsonResult(new { success = false, message = "Thông tin khách hàng không hợp lệ." });
                }

                var response = await client.PostAsJsonAsync("api/Customers", input);
                if (!response.IsSuccessStatusCode)
                {
                    Response.StatusCode = (int)response.StatusCode;
                    return new JsonResult(new
                    {
                        success = false,
                        message = await ReadApiErrorAsync(response, "Không thể tạo khách hàng.")
                    });
                }

                var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
                if (customer == null || customer.CustomerId <= 0)
                    throw new JsonException("The customer API returned no customer identifier.");
                return new JsonResult(new
                {
                    success = true,
                    data = customer
                });
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Invalid customer request or response in the sales-order form.");
                Response.StatusCode = StatusCodes.Status400BadRequest;
                return new JsonResult(new { success = false, message = "Dữ liệu khách hàng không hợp lệ. Vui lòng tải lại trang và thử lại." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot create customer from the sales-order form.");
                Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return new JsonResult(new
                {
                    success = false,
                    message = "Không thể kết nối đến hệ thống dữ liệu."
                });
            }
        }

        private static async Task<string> ReadApiErrorAsync(HttpResponseMessage response, string fallback)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
            if (response.StatusCode == HttpStatusCode.Forbidden)
                return "Tài khoản hiện tại không có quyền thực hiện thao tác này.";

            try
            {
                var body = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                if (root.TryGetProperty("message", out var message) &&
                    message.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(message.GetString()))
                    return message.GetString()!;

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    foreach (var property in errors.EnumerateObject())
                        if (property.Value.ValueKind == JsonValueKind.Array)
                            foreach (var item in property.Value.EnumerateArray())
                                if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                                    return item.GetString()!;
            }
            catch (JsonException)
            {
                // A proxy may return HTML for authentication or server failures.
            }

            return $"{fallback} (HTTP {(int)response.StatusCode})";
        }

        private void ValidateBusinessInput()
        {
            if (SalesOrder.ExpectedIssueDate.HasValue &&
                SalesOrder.ExpectedIssueDate.Value < SalesOrder.OrderDate)
            {
                ModelState.AddModelError(
                    "SalesOrder.ExpectedIssueDate",
                    "Ngày xuất dự kiến không được trước ngày đặt hàng.");
            }

            if (SalesOrder.Items == null || SalesOrder.Items.Count == 0)
            {
                ModelState.AddModelError(
                    "SalesOrder.Items",
                    "Đơn bán hàng phải có ít nhất một sản phẩm.");
                return;
            }

            if (SalesOrder.Items.GroupBy(i => i.ProductId).Any(g => g.Count() > 1))
            {
                ModelState.AddModelError(
                    "SalesOrder.Items",
                    "Mỗi sản phẩm chỉ được xuất hiện một lần trong đơn bán hàng.");
            }
        }

        private async Task LoadFormDataAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                var customerResponse = await client.GetAsync("api/Customers");
                if (customerResponse.IsSuccessStatusCode)
                {
                    var customers = await customerResponse.Content
                        .ReadFromJsonAsync<List<CustomerDto>>() ?? new();

                    CustomerOptions = customers.Select(c => new SelectListItem
                    {
                        Text = $"{c.CustomerCode} — {c.CustomerName}",
                        Value = c.CustomerId.ToString()
                    }).ToList();
                }

                CustomerOptions.Insert(0, new SelectListItem("Chọn khách hàng", ""));

                var productResponse = await client.GetAsync("api/SalesOrders/lookups/products");
                if (productResponse.IsSuccessStatusCode)
                {
                    ProductList = await productResponse.Content
                        .ReadFromJsonAsync<List<ProductDto>>() ?? new();
                }
            }
            catch
            {
                ErrorMessage ??= "Không thể tải danh mục khách hàng hoặc sản phẩm.";
            }
        }
    }

    public class CustomerDto
    {
        public long CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class CreateCustomerInput
    {
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Email { get; set; }
    }

    public class ProductDto
    {
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;
        public byte QuantityScale { get; set; }
        public bool TrackLot { get; set; }
        public bool TrackExpiry { get; set; }
        public decimal OnHandQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal? AvailableQuantity { get; set; }
        public long ProductGroupId { get; set; }
        public string ProductGroupName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string RotationMethod { get; set; } = "FIFO";
        public decimal Available => AvailableQuantity ?? 0;
    }

    public class ProductGroupOption
    {
        public long ProductGroupId { get; set; }
        public string ProductGroupName { get; set; } = string.Empty;
    }
}
