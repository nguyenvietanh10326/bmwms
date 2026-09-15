using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

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
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var apiResult = await response.Content.ReadFromJsonAsync<OutboundPagedResponse>(options);
                
                if (apiResult != null && apiResult.Success && apiResult.Data != null)
                {
                    // Filter tasks assigned to the current user that are READY or ISSUING
                    // In a real app, this filtering should be done in the API.
                    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (long.TryParse(userIdClaim, out var currentUserId))
                    {
                        OutboundTasks = apiResult.Data.Items
                            .Where(o => o.AssignedToUserId == currentUserId && (o.Status == "READY" || o.Status == "ISSUING"))
                            .ToList();
                    }
                }
            }

            return Page();
        }
    }

    public class OutboundPagedResponse
    {
        public bool Success { get; set; }
        public OutboundPagedData? Data { get; set; }
    }

    public class OutboundPagedData
    {
        public List<OutboundTaskDto> Items { get; set; } = new();
    }

    public class OutboundTaskDto
    {
        public long OutboundOrderId { get; set; }
        public string OutboundOrderNumber { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public DateTime? ExpectedIssueDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public long? AssignedToUserId { get; set; }
    }
}
