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

    public async Task<SupplierDetailResponseModel?> GetSupplierDetailAsync(string supplierCode)
    {
        var url = $"api/suppliers/{Uri.EscapeDataString(supplierCode)}";
        var response = await _httpClient.GetAsync(url);
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SupplierDetailResponseModel>(content, _jsonOptions);
        }

        return null;
    }

    public async Task<(bool Success, string ErrorMessage)> CreateSupplierAsync(SupplierCreateRequestModel model)
    {
        var content = new StringContent(JsonSerializer.Serialize(model, _jsonOptions), System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/suppliers", content);

        if (response.IsSuccessStatusCode)
        {
            return (true, string.Empty);
        }

        var error = await response.Content.ReadAsStringAsync();
        return (false, error);
    }

    public async Task<(bool Success, string ErrorMessage)> UpdateSupplierAsync(string supplierCode, SupplierUpdateRequestModel model)
    {
        var content = new StringContent(JsonSerializer.Serialize(model, _jsonOptions), System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync($"api/suppliers/{supplierCode}", content);

        if (response.IsSuccessStatusCode)
        {
            return (true, string.Empty);
        }

        var error = await response.Content.ReadAsStringAsync();
        return (false, error);
    }
}
