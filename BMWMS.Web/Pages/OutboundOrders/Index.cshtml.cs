using BMWMS.Web.Models.Inventory;
using BMWMS.Web.Models.Warehouse;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMWMS.Web.Pages.OutboundOrders
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Tự động bind từ Query String trên URL
        [BindProperty(SupportsGet = true)]
        public OutboundOrderQueryFilter Filter { get; set; } = new();

        // Kết quả phân trang từ API Backend
        public PagedResultDto<OutboundOrderListDto> PagedResult { get; set; } = new();
        public List<SelectListItem> WarehouseOptions { get; set; } = new();
        // Dropdown trạng thái
        public List<SelectListItem> StatusOptions { get; set; } = new()
        {
            new SelectListItem { Text = "-- Tất cả trạng thái --", Value = "" },
            new SelectListItem { Text = "Nháp (DRAFT)", Value = "DRAFT" },
            new SelectListItem { Text = "Đã phân công (ASSIGNED)", Value = "ASSIGNED" },
            new SelectListItem { Text = "Đang xử lý (IN_PROGRESS)", Value = "IN_PROGRESS" },
            new SelectListItem { Text = "Hoàn thành (COMPLETED)", Value = "COMPLETED" },
            new SelectListItem { Text = "Đã hủy (CANCELLED)", Value = "CANCELLED" }
        };

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        // GET: Gọi API Backend lấy danh sách + phân trang
    public async Task<IActionResult> OnGetAsync()
        {
            Filter.PageIndex = Filter.PageIndex < 1 ? 1 : Filter.PageIndex;
            Filter.PageSize = Filter.PageSize < 1 ? 10 : Filter.PageSize;

            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                // ==========================================
                // 1. Lấy danh sách kho
                // ==========================================
                var warehouseResponse = await client.GetAsync("AllWarehouse");

                WarehouseOptions = new List<SelectListItem>
        {
            new SelectListItem
            {
                Text = "Tất cả kho",
                Value = ""
            }
        };

                if (warehouseResponse.IsSuccessStatusCode)
                {
                    var warehouses =
                        await warehouseResponse.Content
                            .ReadFromJsonAsync<List<WarehouseDto>>();

                    if (warehouses != null)
                    {
                        WarehouseOptions.AddRange(
                            warehouses.Select(w => new SelectListItem
                            {
                                Text = $"{w.WarehouseCode} - {w.WarehouseName}",
                                Value = w.WarehouseId.ToString(),
                                Selected = Filter.WarehouseId == w.WarehouseId
                            })
                        );
                    }
                }

                // ==========================================
                // 2. Lấy danh sách phiếu xuất
                // ==========================================
                string queryString =
                    $"?Search={Uri.EscapeDataString(Filter.Search ?? "")}" +
                    $"&Status={Uri.EscapeDataString(Filter.Status ?? "")}" +
                    $"&WarehouseId={Filter.WarehouseId}" +
                    $"&PageIndex={Filter.PageIndex}" +
                    $"&PageSize={Filter.PageSize}";

                var response =
                    await client.GetAsync($"api/OutboundOrders{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    PagedResult =
                        await response.Content
                            .ReadFromJsonAsync<PagedResultDto<OutboundOrderListDto>>()
                        ?? new PagedResultDto<OutboundOrderListDto>();
                }
                else
                {
                    ErrorMessage = "Không thể lấy dữ liệu từ hệ thống Backend API!";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi kết nối API Backend: {ex.Message}";
            }

            return Page();
        }



        // POST: Gọi API Backend để hủy lệnh xuất kho
        public async Task<IActionResult> OnPostCancelOrderAsync(long id, int pageIndex)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                var response = await client.PostAsync($"api/OutboundOrders/{id}/cancel", null);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Hủy phiếu xuất kho thành công!";
                }
                else
                {
                    var errorData = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    ErrorMessage = errorData?.Message ?? "Lỗi từ server khi thực hiện hủy phiếu!";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi kết nối API Backend: {ex.Message}";
            }

            // Redirect reload lại trang kèm giữ nguyên bộ lọc & trang hiện tại
            return RedirectToPage("./Index", new
            {
                Search = Filter.Search,
                Status = Filter.Status,
                WarehouseId = Filter.WarehouseId,
                PageIndex = pageIndex
            });
        }
    }

    public class ErrorResponse
    {
        public string? Message { get; set; }
    }
}
