using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BMWMS.Web.Models;

namespace BMWMS.Web.Services;

public class InboundApiService
{
    private readonly HttpClient _httpClient;

    public InboundApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
    }

    public async Task<InboundOrderPageModel> GetInboundOrdersPageAsync(InboundOrderFilterModel filter)
    {
        var queryString = $"?pageIndex={filter.PageIndex}&pageSize={filter.PageSize}";
        if (!string.IsNullOrEmpty(filter.Keyword)) queryString += $"&keyword={filter.Keyword}";
        if (!string.IsNullOrEmpty(filter.Status)) queryString += $"&status={filter.Status}";
        if (filter.FromDate.HasValue) queryString += $"&fromDate={filter.FromDate.Value:yyyy-MM-dd}";
        if (filter.ToDate.HasValue) queryString += $"&toDate={filter.ToDate.Value:yyyy-MM-dd}";

        var response = await _httpClient.GetFromJsonAsync<InboundOrderPageModel>($"api/inbounds{queryString}");
        return response ?? new InboundOrderPageModel();
    }

    public async Task<InboundOrderDetailDto?> GetInboundOrderByIdAsync(long id)
    {
        return await _httpClient.GetFromJsonAsync<InboundOrderDetailDto>($"api/inbounds/{id}");
    }
}
