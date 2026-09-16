using BMWMS.Web.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Text.Json;
using System;

namespace BMWMS.Web.Services
{
    public class ProductAttributeApiService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public ProductAttributeApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
        }

        public async Task<PagedResultModel<ProductAttributeDto>> GetPagedListAsync(string? keyword, string? status, int pageIndex = 1, int pageSize = 10)
        {
            var queryParams = new List<string>
            {
                $"pageIndex={pageIndex}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(keyword))
                queryParams.Add($"keyword={Uri.EscapeDataString(keyword)}");
            if (!string.IsNullOrWhiteSpace(status))
                queryParams.Add($"status={Uri.EscapeDataString(status)}");

            var queryString = string.Join("&", queryParams);
            var response = await _httpClient.GetAsync($"/api/ProductAttributes?{queryString}");
            if (!response.IsSuccessStatusCode) return new PagedResultModel<ProductAttributeDto>();
            
            return await response.Content.ReadFromJsonAsync<PagedResultModel<ProductAttributeDto>>(_jsonOptions) ?? new PagedResultModel<ProductAttributeDto>();
        }

        public async Task<List<ProductAttributeDto>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("/api/ProductAttributes/all");
            if (!response.IsSuccessStatusCode) return new List<ProductAttributeDto>();
            return await response.Content.ReadFromJsonAsync<List<ProductAttributeDto>>(_jsonOptions) ?? new List<ProductAttributeDto>();
        }

        public async Task<ProductAttributeDto?> GetByIdAsync(long id)
        {
            var response = await _httpClient.GetAsync($"/api/ProductAttributes/{id}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ProductAttributeDto>(_jsonOptions);
        }

        public async Task<ProductAttributeDto?> CreateAsync(CreateProductAttributeDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/ProductAttributes", dto);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(ParseError(error, "Không thể tạo thuộc tính sản phẩm."));
            }
            return await response.Content.ReadFromJsonAsync<ProductAttributeDto>(_jsonOptions);
        }

        public async Task UpdateAsync(long id, UpdateProductAttributeDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/ProductAttributes/{id}", dto);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(ParseError(error, "Không thể cập nhật thuộc tính sản phẩm."));
            }
        }

        public async Task DeleteAsync(long id)
        {
            var response = await _httpClient.DeleteAsync($"/api/ProductAttributes/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(ParseError(error, "Không thể xóa thuộc tính sản phẩm."));
            }
        }

        private static string ParseError(string body, string fallback)
        {
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(body);
                if (json.TryGetProperty("message", out var message))
                    return message.GetString() ?? fallback;
                if (json.TryGetProperty("title", out var title))
                    return title.GetString() ?? fallback;
            }
            catch (JsonException)
            {
            }
            return string.IsNullOrWhiteSpace(body) ? fallback : body;
        }
    }
}
