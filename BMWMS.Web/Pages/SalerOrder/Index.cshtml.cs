using BMWMS.Web.Models.Inventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMWMS.Web.Pages.SalesOrders
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Tự động bind Query Parameters từ URL
        [BindProperty(SupportsGet = true)]
        public SalesOrderSearchCriteria Filter { get; set; } = new();

        // Kết quả phân trang từ API Backend
        public PagedResult<SalesOrderListDto> PagedResult { get; set; } = new();

        public List<SelectListItem> WarehouseOptions { get; set; } = new();

        // Dropdown trạng thái Đơn bán hàng
        public List<SelectListItem> StatusOptions { get; set; } = new()
        {
            new SelectListItem { Text = "-- Tất cả trạng thái --", Value = "" },
            new SelectListItem { Text = "Nháp (DRAFT)", Value = "DRAFT" },
            new SelectListItem { Text = "Đã giữ tồn (ALLOCATED)", Value = "ALLOCATED" },
            new SelectListItem { Text = "Đã xác nhận (CONFIRMED)", Value = "CONFIRMED" },
            new SelectListItem { Text = "Hoàn tất (FULFILLED)", Value = "FULFILLED" },
            new SelectListItem { Text = "Đã hủy (CANCELLED)", Value = "CANCELLED" }
        };

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        // GET: Gọi API Backend lấy danh sách Sales Order + Phân trang
        public async Task<IActionResult> OnGetAsync()
        {
            Filter.PageIndex = Filter.PageIndex < 1 ? 1 : Filter.PageIndex;
            Filter.PageSize = Filter.PageSize < 1 ? 10 : Filter.PageSize;

            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                string queryString =
                    $"?Keyword={Uri.EscapeDataString(Filter.Keyword ?? "")}" +
                    $"&Status={Uri.EscapeDataString(Filter.Status ?? "")}" +
                    $"&PageIndex={Filter.PageIndex}" +
                    $"&PageSize={Filter.PageSize}";

                var response = await client.GetAsync($"salesorders{queryString}");

                if (response.IsSuccessStatusCode)
                {
                    // Trường hợp Backend API trả dạng { success: true, data: PagedResult }
                    var apiWrapper = await response.Content.ReadFromJsonAsync<ApiWrapper<PagedResult<SalesOrderListDto>>>();
                    if (apiWrapper != null && apiWrapper.Data != null)
                    {
                        PagedResult = apiWrapper.Data;
                    }
                    else
                    {
                        // Trường hợp Backend API trả trực tiếp PagedResult
                        PagedResult = await response.Content.ReadFromJsonAsync<PagedResult<SalesOrderListDto>>()
                                      ?? new PagedResult<SalesOrderListDto>();
                    }
                }
                else
                {
                    ErrorMessage = "Không thể lấy dữ liệu Đơn bán hàng từ hệ thống Backend API!";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi kết nối API Backend: {ex.Message}";
            }

            return Page();
        }

        // POST: Gọi API Backend để hủy đơn bán hàng
        public async Task<IActionResult> OnPostCancelOrderAsync(long id, int pageIndex)
        {
            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                var response = await client.PostAsync($"api/SalesOrders/{id}/cancel", null);

                if (response.IsSuccessStatusCode)
                {
                    SuccessMessage = "Hủy đơn bán hàng thành công!";
                }
                else
                {
                    var errorData = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    ErrorMessage = errorData?.Message ?? "Lỗi từ server khi thực hiện hủy đơn!";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi kết nối API Backend: {ex.Message}";
            }

            // Redirect reload lại trang kèm giữ nguyên bộ lọc & trang hiện tại
            return RedirectToPage("./Index", new
            {
                Keyword = Filter.Keyword,
                Status = Filter.Status,
                PageIndex = pageIndex
            });
        }
    }

    public class ApiWrapper<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    public class ErrorResponse
    {
        public string? Message { get; set; }
    }
}