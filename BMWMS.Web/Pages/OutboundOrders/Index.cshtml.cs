using BMWMS.Web.Models.Inventory;
using BMWMS.Web.Models.Warehouse;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BMWMS.Web.Pages.OutboundOrders
{
    [Authorize(Roles = "SYSTEM_ADMIN,WAREHOUSE_MANAGER,WAREHOUSE_STAFF,SALES_STAFF,PURCHASING_STAFF")]
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
        // Dropdown trạng thái
        public List<SelectListItem> StatusOptions { get; set; } = new()
        {
            new SelectListItem { Text = "-- Tất cả trạng thái --", Value = "" },
            new SelectListItem { Text = "Nháp (DRAFT)", Value = "DRAFT" },
            new SelectListItem { Text = "Sẵn sàng xuất", Value = "READY" },
            new SelectListItem { Text = "Đang xuất hàng", Value = "ISSUING" },
            new SelectListItem { Text = "Đã xuất", Value = "ISSUED" },
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
                string queryString =
                    $"?Search={Uri.EscapeDataString(Filter.Search ?? "")}" +
                    $"&Status={Uri.EscapeDataString(Filter.Status ?? "")}" +
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
                    ErrorMessage = "Không thể tải danh sách phiếu xuất kho.";
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Không thể kết nối đến hệ thống. Vui lòng thử lại.";
            }

            return Page();
        }
    }
}
