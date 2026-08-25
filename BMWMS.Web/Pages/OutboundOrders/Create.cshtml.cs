using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace BMWMS.Web.Pages.OutboundOrders
{
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,SALES_STAFF")]
    public class CreateModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public CreateModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty]
        public OutboundOrderVM Input { get; set; } = new();

        public List<SelectListItem> SalesOrderOptions { get; set; } = new();
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<SelectListItem> AssigneeOptions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(long? selectedSalesOrderId)
        {
            Input.ExpectedIssueDate = DateTime.Now;

            await LoadDropdownsAsync();

            if (selectedSalesOrderId.HasValue && selectedSalesOrderId.Value > 0)
            {
                Input.SalesOrderId = selectedSalesOrderId.Value;
                await LoadSalesOrderDetailAsync(selectedSalesOrderId.Value);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string actionType)
        {
            if (!ModelState.IsValid)
            {
                await ReloadDropdownsAndItemsAsync();
                return Page();
            }

            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                // Chuẩn bị Request DTO đúng theo API Backend
                var apiPayload = new CreateOutboundOrderRequest
                {
                    WarehouseId = Input.WarehouseId,
                    SourceType = "SALES_ORDER",
                    SalesOrderId = Input.SalesOrderId,
                    ExpectedIssueDate = Input.ExpectedIssueDate.ToString("yyyy-MM-dd"),
                    AssignedToUserId = Input.AssignedToUserId,
                    Notes = Input.ReferenceCode,
                    IsSubmit = actionType == "submit", // IsSubmit = true khi nhấn "Tạo lệnh xuất", false khi "Lưu nháp"
                    Items = Input.Items.Select(x => new OutboundOrderItemRequest
                    {
                        ProductId = x.ProductId,
                        RequestedQuantity = x.RequestedQuantity,
                        Notes = x.Notes
                    }).ToList()
                };

                var response = await client.PostAsJsonAsync("api/OutboundOrders", apiPayload);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = actionType == "submit" ? "Tạo lệnh xuất kho thành công!" : "Lưu nháp lệnh xuất kho thành công!";
                    return RedirectToPage("./Index");
                }
                // Debug: Kiểm tra ID trước khi POST
                Console.WriteLine($"SalesOrderId gửi đi: {Input.SalesOrderId}");
                foreach (var item in Input.Items)
                {
                    Debug.WriteLine($"-> Item ProductId: {item.ProductId}, SL: {item.RequestedQuantity}");
                }
                if (!response.IsSuccessStatusCode)
                {
                    // Đọc toàn bộ nội dung lỗi chi tiết trả về từ Backend API
                    var errorContent = await response.Content.ReadAsStringAsync();

                    // Hiển thị trực tiếp lỗi từ Backend ra thông báo UI
                    ModelState.AddModelError(string.Empty, $"Lỗi Backend ({response.StatusCode}): {errorContent}");

                    await ReloadDropdownsAndItemsAsync();
                    return Page();
                }
                var errorObj = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
                ModelState.AddModelError(string.Empty, errorObj?.Message ?? errorObj?.Detail ?? "Không thể tạo lệnh xuất kho.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi hệ thống: {ex.Message}");
            }

            await ReloadDropdownsAndItemsAsync();
            return Page();
        }

        private async Task ReloadDropdownsAndItemsAsync()
        {
            await LoadDropdownsAsync();
            if ((Input.Items == null || !Input.Items.Any()) && Input.SalesOrderId > 0)
            {
                await LoadSalesOrderDetailAsync(Input.SalesOrderId);
            }
        }

        private async Task LoadDropdownsAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                // 1. Gọi API Lấy danh sách Sales Orders đã xác nhận
                var salesOrders = await client.GetFromJsonAsync<List<SalesOrderOptionDto>>("api/OutboundOrders/sales-orders") ?? new();
                SalesOrderOptions = salesOrders.Select(x => new SelectListItem(x.SalesOrderNumber, x.SalesOrderId.ToString())).ToList();

                // 2. Gọi API Lấy danh sách Người tạo/Người phụ trách
                var creators = await client.GetFromJsonAsync<List<UserOptionDto>>("api/OutboundOrders/creators") ?? new();
                AssigneeOptions = creators.Select(x => new SelectListItem(x.FullName ?? x.Username, x.UserId.ToString())).ToList();

                // 3. Gọi API Lấy danh sách Kho thật từ Backend (/AllWarehouse)
                var warehouses = await client.GetFromJsonAsync<List<WarehouseOptionDto>>("AllWarehouse") ?? new();
                WarehouseOptions = warehouses.Select(x => new SelectListItem(
                    text: $"{x.WarehouseName} ({x.WarehouseCode})",
                    value: x.WarehouseId.ToString()
                )).ToList();
            }
            catch
            {
                SalesOrderOptions = new();
                AssigneeOptions = new();
                WarehouseOptions = new();
            }

            SalesOrderOptions.Insert(0, new SelectListItem("-- Chọn Sales Order --", ""));
            AssigneeOptions.Insert(0, new SelectListItem("-- Chọn người phụ trách --", ""));
            WarehouseOptions.Insert(0, new SelectListItem("-- Chọn kho xuất --", ""));
        }

        private async Task LoadSalesOrderDetailAsync(long salesOrderId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                var response = await client.GetAsync($"api/OutboundOrders/sales-order/{salesOrderId}");

                if (response.IsSuccessStatusCode)
                {
                    var detail = await response.Content.ReadFromJsonAsync<SalesOrderDetailApiResponse>();
                    if (detail != null)
                    {
                        Input.CustomerName = detail.CustomerName;
                        Input.ReferenceCode = detail.SalesOrderNumber;
                        if (detail.WarehouseId.HasValue && detail.WarehouseId.Value > 0)
                        {
                            Input.WarehouseId = detail.WarehouseId.Value;
                        }

                        Input.Items = detail.Items.Select(x => new OutboundOrderItemVM
                        {
                            ProductId = x.ProductId,
                            ProductCode = x.ProductCode,
                            ProductName = x.ProductName,
                            // Sửa chỗ này: Nếu UnitOfMeasure bị null từ API thì gán mặc định là "Cái" hoặc "N/A"
                            UnitName = string.IsNullOrWhiteSpace(x.UnitOfMeasure) ? "N/A" : x.UnitOfMeasure,
                            RequestedQuantity = x.Quantity,
                            ReservedQuantity = x.ReservedQuantity,
                            LotBin = x.LotBin ?? "N/A",
                            TotalPrice = x.UnitPrice * x.Quantity
                        }).ToList();
                    }
                }
            }
            catch
            {
                Input.Items = new List<OutboundOrderItemVM>();
            }
        }
    }

    #region ViewModels & DTOs Map Chi Tiết Với Backend
    public class WarehouseOptionDto
    {
        public long WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
    }
    public class OutboundOrderVM
    {
        [Required(ErrorMessage = "Vui lòng chọn Sales Order.")]
        public long SalesOrderId { get; set; }
        public string? UnitName { get; set; } = string.Empty;
        public string? CustomerName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Kho xuất.")]
        public long WarehouseId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Ngày dự kiến.")]
        public DateTime ExpectedIssueDate { get; set; } = DateTime.Now;

        public long? AssignedToUserId { get; set; }
        public string? ReferenceCode { get; set; }

        public List<OutboundOrderItemVM> Items { get; set; } = new();
    }

    public class OutboundOrderItemVM
    {
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public decimal RequestedQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public string LotBin { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
    }

    // Các DTO truyền nhận Payload với Backend Controller
    public class CreateOutboundOrderRequest
    {
        public long WarehouseId { get; set; }
        public string SourceType { get; set; } = "SALES_ORDER";
        public long SalesOrderId { get; set; }
        public string ExpectedIssueDate { get; set; } = string.Empty;
        public long? AssignedToUserId { get; set; }
        public string? Notes { get; set; }
        public bool IsSubmit { get; set; }
        public List<OutboundOrderItemRequest> Items { get; set; } = new();
    }

    public class OutboundOrderItemRequest
    {
        public long ProductId { get; set; }
        public decimal RequestedQuantity { get; set; }
        public string? Notes { get; set; }
    }

    public class SalesOrderOptionDto
    {
        public long SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
    }

    public class UserOptionDto
    {
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
    }

    public class SalesOrderDetailApiResponse
    {
        public long SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public long? WarehouseId { get; set; }
        public List<SalesOrderItemApiResponse> Items { get; set; } = new();
    }

    public class SalesOrderItemApiResponse
    {
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public string? LotBin { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class ApiErrorResponse
    {
        public string? Message { get; set; }
        public string? Detail { get; set; }
    }

    #endregion
}
