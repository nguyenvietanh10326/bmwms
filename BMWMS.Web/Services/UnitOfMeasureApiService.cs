using BMWMS.Web.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BMWMS.Web.Services
{
    public class UnitOfMeasureApiService
    {
        private readonly HttpClient _httpClient;

        public UnitOfMeasureApiService(IHttpClientFactory factory)
        {
            _httpClient = factory.CreateClient("ApiClient");
        }

        public async Task<List<UnitOfMeasureDto>> GetAllAsync(string? keyword = null)
        {
            var url = "/api/UnitOfMeasures";
            if (!string.IsNullOrEmpty(keyword)) url += $"?keyword={keyword}";
            
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<UnitOfMeasureDto>>() ?? new List<UnitOfMeasureDto>();
        }

        public async Task<UnitOfMeasureDto?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"/api/UnitOfMeasures/{id}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<UnitOfMeasureDto>();
        }

        public async Task<(bool Success, string Message)> CreateAsync(CreateUnitOfMeasureDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/UnitOfMeasures", dto);
            if (!response.IsSuccessStatusCode)
            {
                var error = await ApiErrorReader.ReadAsync(response);
                return (false, error);
            }
            return (true, "Thêm đơn vị tính thành công.");
        }

        public async Task<(bool Success, string Message)> UpdateAsync(int id, UpdateUnitOfMeasureDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/UnitOfMeasures/{id}", dto);
            if (!response.IsSuccessStatusCode)
            {
                var error = await ApiErrorReader.ReadAsync(response);
                return (false, error);
            }
            return (true, "Cập nhật đơn vị tính thành công.");
        }

        public async Task<(bool Success, string Message)> DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"/api/UnitOfMeasures/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var error = await ApiErrorReader.ReadAsync(response);
                return (false, error);
            }
            return (true, "Xóa đơn vị tính thành công.");
        }
    }
}
