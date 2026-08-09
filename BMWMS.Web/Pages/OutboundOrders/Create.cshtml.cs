using BMWMS.Web.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Json;

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
        public CreateOutboundOrderInput Input { get; set; } = new();

        // Dropdowns hiển thị trên UI
        public List<SelectListItem> SalesOrderOptions { get; set; } = new();
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        public List<SelectListItem> UserOptions { get; set; } = new();
        public List<SelectListItem> PickingStrategyOptions { get; set; } = new()
        {
            new SelectListItem { Text = "FEFO", Value = "FEFO" },
            new SelectListItem { Text = "FIFO", Value = "FIFO" },
            new SelectListItem { Text = "LIFO", Value = "LIFO" }
        };

        [TempData]
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(long? selectedSalesOrderId)
        {
            // 1. Gọi API Backend để load tất cả các danh sách Dropdown
            await LoadDropdownsFromApiAsync();

            Input.ExpectedIssueDate = DateTime.Now.AddDays(1);
            Input.PickingStrategy = "FEFO";

            // 2. Nếu người dùng chọn Sales Order -> Gọi API lấy thông tin chi tiết của Sales Order đó
            if (selectedSalesOrderId.HasValue && selectedSalesOrderId > 0)
            {
                Input.SalesOrderId = selectedSalesOrderId.Value;
                await LoadSalesOrderDetailFromApiAsync(selectedSalesOrderId.Value);
            }

            return Page();
        }

        // Submit Form Tạo mới / Lưu nháp sang API Backend
        public async Task<IActionResult> OnPostAsync(string actionType)
        {
            if (Input.SalesOrderId <= 0)
            {
                ErrorMessage = "Vui lòng chọn Sales Order!";
                await LoadDropdownsFromApiAsync();
                return Page();
            }

            var client = _httpClientFactory.CreateClient("ApiClient");

            // Mapping dữ liệu từ UI gửi về Request DTO cho API OutboundOrder
            var request = new CreateOutboundOrderRequest
            {
                SourceType = "SALES_ORDER",
                SalesOrderId = Input.SalesOrderId,
                WarehouseId = Input.WarehouseId,
                ExpectedIssueDate = Input.ExpectedIssueDate,
                AssignedToUserId = Input.AssignedToUserId,
                Notes = Input.Notes,
                IsSubmit = (actionType == "submit"), // submit -> ASSIGNED/IN_PROGRESS, draft -> DRAFT
                Items = Input.Items.Select(i => new CreateOutboundOrderItemRequest
                {
                    ProductId = i.ProductId,
                    RequestedQuantity = i.RequestedQuantity,
                    Notes = i.Notes
                }).ToList()
            };

            try
            {
                var response = await client.PostAsJsonAsync("api/OutboundOrders", request);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = actionType == "submit"
                        ? "Tạo lệnh xuất kho thành công!"
                        : "Lưu nháp lệnh xuất kho thành công!";

                    return RedirectToPage("./Index");
                }
                else
                {
                    var errorObj = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    ErrorMessage = errorObj?.Message ?? "Lỗi từ Server khi tạo lệnh xuất kho!";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi kết nối API Backend: {ex.Message}";
            }

            await LoadDropdownsFromApiAsync();
            return Page();
        }

        #region Gọi API lấy dữ liệu thực tế (Không Hardcode)

        private async Task LoadDropdownsFromApiAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            // Khởi tạo trước mục mặc định cho Sales Order Dropdown
            SalesOrderOptions = new List<SelectListItem>
            {
                new SelectListItem { Text = "-- Chọn Sales Order --", Value = "" }
            };

            try
            {
                // A. Gọi API lấy danh sách Kho
                var warehouses = await client.GetFromJsonAsync<List<WarehouseApiResponse>>("AllWarehouse");
                if (warehouses != null)
                {
                    WarehouseOptions = warehouses.Select(w => new SelectListItem
                    {
                        Text = w.WarehouseName,
                        Value = w.WarehouseId.ToString()
                    }).ToList();
                }

                // B. Gọi API lấy danh sách Sales Order đã xác nhận (Dùng endpoint mới tạo bên Repo BE)
                var salesOrders = await client.GetFromJsonAsync<List<SalesOrderApiResponse>>("api/OutboundOrders/sales-orders");
                if (salesOrders != null && salesOrders.Any())
                {
                    var items = salesOrders.Select(s => new SelectListItem
                    {
                        Text = s.SalesOrderNumber,
                        Value = s.SalesOrderId.ToString()
                    });
                    SalesOrderOptions.AddRange(items);
                }

                // C. Gọi API lấy danh sách Nhân viên phụ trách
                var users = await client.GetFromJsonAsync<List<UserApiResponse>>("api/OutboundOrders/creators");
                if (users != null)
                {
                    UserOptions = users.Select(u => new SelectListItem
                    {
                        Text = !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.Username,
                        Value = u.UserId.ToString()
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Không thể tải dữ liệu danh mục từ API: {ex.Message}";
            }
        }

        private async Task LoadSalesOrderDetailFromApiAsync(long salesOrderId)
        {
            // ĐÃ SỬA: Đồng bộ dùng "ApiClient"
            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                // Gọi API lấy Chi tiết Sales Order theo ID
                var soDetail = await client.GetFromJsonAsync<SalesOrderDetailApiResponse>($"api/OutboundOrders/sales-order/{salesOrderId}");

                if (soDetail != null)
                {
                    // Đổ dữ liệu từ API vào Input Model
                    Input.CustomerName = soDetail.CustomerName;
                    Input.ReferenceCode = soDetail.SalesOrderNumber;
                    if (soDetail.WarehouseId.HasValue && soDetail.WarehouseId > 0)
                    {
                        Input.WarehouseId = soDetail.WarehouseId.Value;
                    }

                    // Tự động map danh sách mặt hàng từ Sales Order
                    Input.Items = soDetail.Items.Select(item => new CreateItemRowInput
                    {
                        ProductId = item.ProductId,
                        ProductCode = item.ProductCode,
                        ProductName = item.ProductName,
                        RequestedQuantity = item.Quantity,
                        ReservedQuantity = item.ReservedQuantity, // Số lượng đã giữ tồn từ SO
                        UnitName = item.UnitName,
                        LotBin = item.LotBinInfo ?? "N/A",
                        CheckResult = item.ReservedQuantity >= item.Quantity ? "Đủ" : $"Thiếu {item.Quantity - item.ReservedQuantity}",
                        IsEnough = item.ReservedQuantity >= item.Quantity,
                        TotalPrice = item.UnitPrice * item.Quantity
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi khi tải chi tiết Sales Order từ API: {ex.Message}";
            }
        }

        #endregion
    }
}