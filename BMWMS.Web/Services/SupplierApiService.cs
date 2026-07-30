using System.Text.Json;
using BMWMS.Web.Models;

namespace BMWMS.Web.Services;

public class SupplierApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public SupplierApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("ApiClient");
        _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    }

    public async Task<PagedResultModel<SupplierListResponseModel>?> GetPagedListAsync(SupplierFilterModel filter)
    {
        var query = new List<string>();
        if (!string.IsNullOrEmpty(filter.Keyword))
            query.Add($"Keyword={Uri.EscapeDataString(filter.Keyword)}");
        if (!string.IsNullOrEmpty(filter.Status))
            query.Add($"Status={Uri.EscapeDataString(filter.Status)}");
        
        query.Add($"PageIndex={filter.PageIndex}");
        query.Add($"PageSize={filter.PageSize}");

        var queryString = string.Join("&", query);
        var url = $"api/suppliers?{queryString}";

        var response = await _httpClient.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            File.WriteAllText("d:\\bmwms\\debug_json.txt", content);
            return JsonSerializer.Deserialize<PagedResultModel<SupplierListResponseModel>>(content, _jsonOptions);
        }

        return new PagedResultModel<SupplierListResponseModel>();
    }
}
