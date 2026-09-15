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

        public async Task<List<ProductAttributeDto>> GetAllAsync()
        {
            var response = await _httpClient.GetAsync("/api/ProductAttributes");
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
                throw new InvalidOperationException($"Lỗi tạo thông số: {error}");
            }
            return await response.Content.ReadFromJsonAsync<ProductAttributeDto>(_jsonOptions);
        }

        public async Task UpdateAsync(long id, UpdateProductAttributeDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/ProductAttributes/{id}", dto);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Lỗi cập nhật thông số: {error}");
            }
        }

        public async Task DeleteAsync(long id)
        {
            var response = await _httpClient.DeleteAsync($"/api/ProductAttributes/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Lỗi xóa thông số: {error}");
            }
        }
    }
}
