using BMWMS.Web.Models.Warehouse;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace BMWMS.Web.Pages.Warehouse
{
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public IndexModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [BindProperty(SupportsGet = true)]
        public WarehouseFilterDto Filter { get; set; } = new WarehouseFilterDto();

        public PagedResultDto<WarehouseResponseDto> WarehousesResult { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (Filter.PageIndex < 1) Filter.PageIndex = 1;
            if (Filter.PageSize < 1) Filter.PageSize = 10;

            var client = _httpClientFactory.CreateClient("ApiClient");

            var queryParams = new List<string>
            {
                $"pageIndex={Filter.PageIndex}",
                $"pageSize={Filter.PageSize}"
            };

            if (!string.IsNullOrWhiteSpace(Filter.Keyword))
                queryParams.Add($"keyword={Uri.EscapeDataString(Filter.Keyword)}");

            if (!string.IsNullOrWhiteSpace(Filter.Status))
                queryParams.Add($"status={Uri.EscapeDataString(Filter.Status)}");

            if (Filter.IsPrimary.HasValue)
                queryParams.Add($"isPrimary={Filter.IsPrimary.Value}");

            var url = $"api/warehouses?{string.Join("&", queryParams)}";

            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                WarehousesResult = JsonSerializer.Deserialize<PagedResultDto<WarehouseResponseDto>>(content, options)
                                   ?? new PagedResultDto<WarehouseResponseDto>();
            }
        }
    }
}
