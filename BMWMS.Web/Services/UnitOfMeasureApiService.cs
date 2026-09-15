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

        public UnitOfMeasureApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
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
            return await _httpClient.GetFromJsonAsync<UnitOfMeasureDto>($"/api/UnitOfMeasures/{id}");
        }

        public async Task CreateAsync(CreateUnitOfMeasureDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/UnitOfMeasures", dto);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new System.Exception(error);
            }
        }

        public async Task UpdateAsync(int id, UpdateUnitOfMeasureDto dto)
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/UnitOfMeasures/{id}", dto);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new System.Exception(error);
            }
        }

        public async Task DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"/api/UnitOfMeasures/{id}");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new System.Exception(error);
            }
        }
    }
}

