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
        var queryString = $"Keyword={filter.Keyword}&Status={filter.Status}&PageIndex={filter.PageIndex}&PageSize={filter.PageSize}";
        
        if (filter.FromDate.HasValue)
        {
            queryString += $"&FromDate={filter.FromDate.Value:yyyy-MM-dd}";
        }
        if (filter.ToDate.HasValue)
        {
            queryString += $"&ToDate={filter.ToDate.Value:yyyy-MM-dd}";
        }

        return await _httpClient.GetFromJsonAsync<InboundOrderPageModel>($"api/inbounds?{queryString}") 
               ?? new InboundOrderPageModel();
    }
}
