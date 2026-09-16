using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BMWMS.Web.Models.Inventory;
using BMWMS.Web.Models.Warehouse;

namespace BMWMS.Web.Pages.Warehouse
{
    [Authorize(Roles = "WAREHOUSE_STAFF")]
    public class MyTasksModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MyTasksModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public List<OutboundTaskDto> OutboundTasks { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var client = _httpClientFactory.CreateClient("ApiClient");
            var response = await client.GetAsync("api/OutboundOrders?PageIndex=1&PageSize=100"); // A quick fetch

            if (response.IsSuccessStatusCode)
            {
                // API đã giới hạn theo người đang đăng nhập; phản hồi là PagedResultDto trực tiếp,
                // không có lớp bọc success/data.
                var apiResult = await response.Content.ReadFromJsonAsync<PagedResultDto<OutboundOrderListDto>>();
                OutboundTasks = (apiResult?.Items ?? new List<OutboundOrderListDto>())
                    .Where(order => order.Status is "READY" or "ISSUING")
                    .Select(order => new OutboundTaskDto
                    {
                        OutboundOrderId = order.OutboundOrderId,
                        OutboundOrderNumber = order.OutboundOrderNumber,
                        PartnerName = order.PartnerName,
                        ExpectedIssueDate = order.ExpectedIssueDate.ToDateTime(TimeOnly.MinValue),
                        Status = order.Status
                    })
                    .ToList();
            }

            return Page();
        }
    }

    public class OutboundTaskDto
    {
        public long OutboundOrderId { get; set; }
        public string OutboundOrderNumber { get; set; } = string.Empty;
        public string? PartnerName { get; set; }
        public DateTime? ExpectedIssueDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
