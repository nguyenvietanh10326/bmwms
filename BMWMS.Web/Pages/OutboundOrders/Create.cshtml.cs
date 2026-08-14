using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace BMWMS.Web.Pages.OutboundOrders
{
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

        /// <summary>
        /// Handler AJAX gọi từ JavaScript khi User chọn Sales Order từ dropdown trên UI
        /// </summary>
        public async Task<IActionResult> OnGetSalesOrderDetailAsync(long id)
        {
            if (id <= 0)
            {
                return new JsonResult(new { success = false, message = "SalesOrderId không hợp lệ." });
            }

            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");
                var response = await client.GetAsync($"api/OutboundOrders/sales-order/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var detail = await response.Content.ReadFromJsonAsync<SalesOrderDetailApiResponse>();
                    return new JsonResult(new { success = true, data = detail });
                }

                return new JsonResult(new { success = false, message = "Không tìm thấy chi tiết Sales Order từ server." });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Lỗi kết nối API: {ex.Message}" });
            }
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
                    IsSubmit = actionType == "submit", // IsSubmit = true khi "Tạo lệnh", false khi "Lưu nháp"
                    Items = Input.Items.Select(x => new OutboundOrderItemRequest
                    {
                        ProductId = x.ProductId,
                        RequestedQuantity = x.RequestedQuantity,
                        Notes = x.Notes
                    }).ToList()
                };

                // Log debug kiểm tra dữ liệu trước khi gửi
                Debug.WriteLine($"POST API OutboundOrders -> SalesOrderId: {Input.SalesOrderId}, IsSubmit: {apiPayload.IsSubmit}");

                var response = await client.PostAsJsonAsync("api/OutboundOrders", apiPayload);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = actionType == "submit"
                        ? "Tạo lệnh xuất kho thành công!"
                        : "Lưu nháp lệnh xuất kho thành công!";

                    return RedirectToPage("./Index");
                }

                // Xử lý đọc đọc lỗi trả về từ Backend API
                var errorContent = await response.Content.ReadAsStringAsync();
                string errorMessage = "Không thể tạo lệnh xuất kho.";

                try
                {
                    var errorObj = System.Text.Json.JsonSerializer.Deserialize<ApiErrorResponse>(errorContent, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (!string.IsNullOrWhiteSpace(errorObj?.Message)) errorMessage = errorObj.Message;
                    else if (!string.IsNullOrWhiteSpace(errorObj?.Detail)) errorMessage = errorObj.Detail;
                    else if (!string.IsNullOrWhiteSpace(errorContent)) errorMessage = errorContent;
                }
                catch
                {
                    if (!string.IsNullOrWhiteSpace(errorContent)) errorMessage = errorContent;
                }

                ModelState.AddModelError(string.Empty, $"Lỗi từ Server ({response.StatusCode}): {errorMessage}");
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

                // 1. Gọi API Lấy danh sách Sales Orders
                var salesOrders = await client.GetFromJsonAsync<List<SalesOrderOptionDto>>("api/OutboundOrders/sales-orders") ?? new();
                SalesOrderOptions = salesOrders.Select(x => new SelectListItem(x.SalesOrderNumber, x.SalesOrderId.ToString())).ToList();

                // 2. Gọi API Lấy danh sách Người phụ trách
                var creators = await client.GetFromJsonAsync<List<UserOptionDto>>("api/OutboundOrders/creators") ?? new();
                AssigneeOptions = creators.Select(x => new SelectListItem(x.FullName ?? x.Username, x.UserId.ToString())).ToList();

                // 3. Gọi API Lấy danh sách Kho thật từ Backend
                var warehouses = await client.GetFromJsonAsync<List<WarehouseOptionDto>>("AllWarehouse") ?? new();
                WarehouseOptions = warehouses.Select(x => new SelectListItem(
                    text: $"{x.WarehouseName} ({x.WarehouseCode})",
                    value: x.WarehouseId.ToString()
                )).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi tải Dropdowns: {ex.Message}");
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
                            UnitName = string.IsNullOrWhiteSpace(x.UnitOfMeasure) ? "N/A" : x.UnitOfMeasure,
                            RequestedQuantity = x.Quantity,
                            ReservedQuantity = x.ReservedQuantity,
                            LotBin = x.LotBin ?? "N/A",
                            TotalPrice = x.UnitPrice * x.Quantity
                        }).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi lấy chi tiết SO {salesOrderId}: {ex.Message}");
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