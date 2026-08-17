using BMWMS.Web.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMWMS.Web.Pages.SalesOrders
{
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CreateModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
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

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadFormDataAsync();
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
                var response = await client.PostAsJsonAsync("api/SalesOrders", SalesOrder);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    ErrorMessage = error?.Message ?? "Không thể tạo đơn bán hàng.";
                    await LoadFormDataAsync();
                    return Page();
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
            catch
            {
                ErrorMessage = "Không thể kết nối đến hệ thống dữ liệu. Vui lòng thử lại sau.";
                await LoadFormDataAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCreateCustomerAsync([FromBody] CreateCustomerInput input)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                var response = await client.PostAsJsonAsync("api/Customers", input);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    Response.StatusCode = (int)response.StatusCode;
                    return new JsonResult(new
                    {
                        success = false,
                        message = error?.Message ?? "Không thể tạo khách hàng."
                    });
                }

                var customer = await response.Content.ReadFromJsonAsync<CustomerDto>();
                return new JsonResult(new
                {
                    success = true,
                    data = customer
                });
            }
            catch
            {
                Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return new JsonResult(new
                {
                    success = false,
                    message = "Không thể kết nối đến hệ thống dữ liệu."
                });
            }
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
