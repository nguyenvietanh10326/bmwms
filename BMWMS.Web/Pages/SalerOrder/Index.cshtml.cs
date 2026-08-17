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

        [BindProperty(SupportsGet = true)]
        public string? Keyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateOnly? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageIndex { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public PagedResult<SalesOrderListDto> PagedResult { get; set; } = new();

        public List<SelectListItem> StatusOptions { get; } = new()
        {
            new("Tất cả trạng thái", string.Empty),
            new("Nháp", "DRAFT"),
            new("Đã xác nhận", "CONFIRMED"),
            new("Đã xuất một phần", "PARTIALLY_ISSUED"),
            new("Đã xuất", "ISSUED"),
            new("Đã hủy", "CANCELLED")
        };

        public List<SelectListItem> PageSizeOptions { get; } = new()
        {
            new("10 bản ghi", "10"),
            new("20 bản ghi", "20"),
            new("50 bản ghi", "50"),
            new("100 bản ghi", "100")
        };

        public bool HasActiveFilters =>
            !string.IsNullOrWhiteSpace(Keyword) ||
            !string.IsNullOrWhiteSpace(Status) ||
            FromDate.HasValue ||
            ToDate.HasValue;

        [TempData]
        public string? SuccessMessage { get; set; }

        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            PageIndex = Math.Max(1, PageIndex);
            PageSize = PageSize is 10 or 20 or 50 or 100 ? PageSize : 10;

            if (FromDate.HasValue && ToDate.HasValue && FromDate.Value > ToDate.Value)
            {
                ErrorMessage = "Ngày bắt đầu không được sau ngày kết thúc.";
                PagedResult = new PagedResult<SalesOrderListDto>
                {
                    PageIndex = PageIndex,
                    PageSize = PageSize
                };
                return Page();
            }

            var criteria = new SalesOrderSearchCriteria
            {
                Keyword = Keyword?.Trim(),
                Status = Status,
                FromDate = FromDate,
                ToDate = ToDate,
                PageIndex = PageIndex,
                PageSize = PageSize
            };

            var client = _httpClientFactory.CreateClient("ApiClient");

            try
            {
                var queryString =
                    $"?Keyword={Uri.EscapeDataString(criteria.Keyword ?? string.Empty)}" +
                    $"&Status={Uri.EscapeDataString(criteria.Status ?? string.Empty)}" +
                    $"&FromDate={Uri.EscapeDataString(criteria.FromDate?.ToString("yyyy-MM-dd") ?? string.Empty)}" +
                    $"&ToDate={Uri.EscapeDataString(criteria.ToDate?.ToString("yyyy-MM-dd") ?? string.Empty)}" +
                    $"&PageIndex={criteria.PageIndex}" +
                    $"&PageSize={criteria.PageSize}";

                var response = await client.GetAsync($"api/SalesOrders{queryString}");

                if (!response.IsSuccessStatusCode)
                {
                    ErrorMessage = "Không thể tải danh sách đơn bán hàng. Vui lòng thử lại.";
                    return Page();
                }

                var apiWrapper = await response.Content
                    .ReadFromJsonAsync<ApiWrapper<PagedResult<SalesOrderListDto>>>();

                PagedResult = apiWrapper?.Data ?? new PagedResult<SalesOrderListDto>
                {
                    PageIndex = PageIndex,
                    PageSize = PageSize
                };
            }
            catch
            {
                ErrorMessage = "Không thể kết nối đến hệ thống dữ liệu. Vui lòng thử lại sau.";
            }

            return Page();
        }

        public string GetStatusLabel(string? status) => status switch
        {
            "DRAFT" => "Nháp",
            "CONFIRMED" => "Đã xác nhận",
            "PARTIALLY_ISSUED" => "Đã xuất một phần",
            "ISSUED" => "Đã xuất",
            "CANCELLED" => "Đã hủy",
            _ => "Không xác định"
        };

        public string GetStatusBadgeClass(string? status) => status switch
        {
            "DRAFT" => "bg-secondary-subtle text-secondary border-secondary-subtle",
            "CONFIRMED" => "bg-primary-subtle text-primary border-primary-subtle",
            "PARTIALLY_ISSUED" => "bg-warning-subtle text-warning-emphasis border-warning-subtle",
            "ISSUED" => "bg-success-subtle text-success border-success-subtle",
            "CANCELLED" => "bg-danger-subtle text-danger border-danger-subtle",
            _ => "bg-light text-dark border-secondary-subtle"
        };
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
