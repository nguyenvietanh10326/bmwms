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
            OrderDate = DateOnly.FromDateTime(DateTime.Now),
            ExpectedIssueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1))
        };

        // Option cho Dropdown
        public List<SelectListItem> CustomerOptions { get; set; } = new();
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<ProductDto> ProductList { get; set; } = new(); // Dùng cho Modal chọn sản phẩm

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadDropdownDataAsync();
            return Page();
        }

        // POST: Lưu Nháp (DRAFT)
        public async Task<IActionResult> OnPostSaveDraftAsync()
        {
            if (!ModelState.IsValid || SalesOrder.Items == null || !SalesOrder.Items.Any())
            {
                ErrorMessage = "Vui lòng nhập đầy đủ thông tin và chọn ít nhất 1 sản phẩm!";
                await LoadDropdownDataAsync();
                return Page();
            }

            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                var response = await client.PostAsJsonAsync("api/SalesOrders", SalesOrder);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Tạo mới đơn bán hàng (Nháp) thành công!";
                    return RedirectToPage("./Index");
                }

                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                ErrorMessage = error?.Message ?? "Lỗi từ server khi tạo đơn bán hàng!";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi kết nối API Backend: {ex.Message}";
            }

            await LoadDropdownDataAsync();
            return Page();
        }

        // POST: Kiểm tra tồn & Xác nhận (DRAFT -> ALLOCATED)
        public async Task<IActionResult> OnPostConfirmAndReserveAsync()
        {
            if (!ModelState.IsValid || SalesOrder.Items == null || !SalesOrder.Items.Any())
            {
                ErrorMessage = "Vui lòng nhập đầy đủ thông tin và chọn ít nhất 1 sản phẩm!";
                await LoadDropdownDataAsync();
                return Page();
            }

            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                // Bước 1: Tạo đơn nháp trước
                var createResponse = await client.PostAsJsonAsync("api/SalesOrders", SalesOrder);
                if (!createResponse.IsSuccessStatusCode)
                {
                    var error = await createResponse.Content.ReadFromJsonAsync<ErrorResponse>();
                    ErrorMessage = error?.Message ?? "Lỗi từ server khi lưu đơn!";
                    await LoadDropdownDataAsync();
                    return Page();
                }

                var createdResult = await createResponse.Content.ReadFromJsonAsync<ApiWrapper<SalesOrderDetailDto>>();
                long newOrderId = createdResult?.Data?.SalesOrderId ?? 0;

                if (newOrderId <= 0)
                {
                    ErrorMessage = "Không lấy được ID đơn hàng vừa tạo!";
                    await LoadDropdownDataAsync();
                    return Page();
                }

                // Bước 2: Gọi API Kiểm tra tồn kho & Giữ tồn
                var confirmResponse = await client.PostAsync($"api/SalesOrders/{newOrderId}/confirm", null);
                if (confirmResponse.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Đã kiểm tra tồn kho và giữ tồn thành công cho đơn bán hàng!";
                    return RedirectToPage("./Index");
                }

                var confirmError = await confirmResponse.Content.ReadFromJsonAsync<ErrorResponse>();
                ErrorMessage = confirmError?.Message ?? "Không thể giữ tồn kho cho đơn hàng này!";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi kết nối API Backend: {ex.Message}";
            }

            await LoadDropdownDataAsync();
            return Page();
        }

        private async Task LoadDropdownDataAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                // 1. Tải danh sách Khách hàng
                var customerResponse = await client.GetAsync("AllCustomers");
                if (customerResponse.IsSuccessStatusCode)
                {
                    var customers = await customerResponse.Content.ReadFromJsonAsync<List<CustomerDto>>();
                    if (customers != null)
                    {
                        CustomerOptions = customers.Select(c => new SelectListItem
                        {
                            Text = $"{c.CustomerCode} — {c.CustomerName}",
                            Value = c.CustomerId.ToString()
                        }).ToList();
                    }
                }

                // 2. Tải danh sách Kho
                var warehouseResponse = await client.GetAsync("AllWarehouse");
                if (warehouseResponse.IsSuccessStatusCode)
                {
                    var warehouses = await warehouseResponse.Content.ReadFromJsonAsync<List<WarehouseDto>>();
                    if (warehouses != null)
                    {
                        WarehouseOptions = warehouses.Select(w => new SelectListItem
                        {
                            Text = w.WarehouseName,
                            Value = w.WarehouseId.ToString()
                        }).ToList();
                    }
                }

                // 3. Tải danh sách Sản phẩm (Để dùng trong JS Modal chọn nhanh sản phẩm)
                var productResponse = await client.GetAsync("AllProduct");
                if (productResponse.IsSuccessStatusCode)
                {
                    ProductList = await productResponse.Content.ReadFromJsonAsync<List<ProductDto>>() ?? new();
                }
            }
            catch
            {
              
            }
        }
    }

    // DTOs bổ trợ cho Master Data
    public class CustomerDto
    {
        public long CustomerId { get; set; }
        public string CustomerCode { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
    }

    public class ProductDto
    {
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string UnitName { get; set; } = null!;
        public decimal AvailableQuantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}