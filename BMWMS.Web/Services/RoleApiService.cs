using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BMWMS.Web.Models;

namespace BMWMS.Web.Services;

public class RoleApiService
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RoleApiService(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient("ApiClient");
    }

    public async Task<List<RoleModel>> GetActiveRolesAsync()
    {
        var response = await _httpClient.GetAsync("api/role/active");
        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<RoleModel>>(body, _json) ?? new List<RoleModel>();
        }
        return new List<RoleModel>();
    }
}
