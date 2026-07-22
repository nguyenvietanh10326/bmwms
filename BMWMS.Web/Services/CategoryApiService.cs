using BMWMS.Web.Models;
using System.Text.Json;

namespace BMWMS.Web.Services
{
    /// <summary>
    /// Service gọi BMWMS.API — tập trung toàn bộ HttpClient logic tại đây
    /// </summary>
    public class CategoryApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CategoryApiService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public CategoryApiService(IHttpClientFactory factory, ILogger<CategoryApiService> logger)
        {
            _httpClient = factory.CreateClient("ApiClient");
            _logger = logger;
        }

        /// <summary>
        /// Lấy toàn bộ danh sách categories từ API
        /// </summary>
        public async Task<(List<CategoryViewModel> Data, string? Error)> GetAllAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/categories");

                if (!response.IsSuccessStatusCode)
                {
                    var msg = $"API trả về lỗi: {(int)response.StatusCode} {response.ReasonPhrase}";
                    _logger.LogWarning(msg);
                    return ([], msg);
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<List<CategoryViewModel>>(content, _jsonOptions) ?? [];
                return (data, null);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Không kết nối được API");
                return ([], $"Không thể kết nối tới API: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định khi gọi GetAllCategories");
                return ([], $"Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy 1 category theo ID
        /// </summary>
        public async Task<(CategoryViewModel? Data, string? Error)> GetByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/categories/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return (null, $"Không tìm thấy category ID = {id}");

                if (!response.IsSuccessStatusCode)
                    return (null, $"API lỗi: {(int)response.StatusCode}");

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<CategoryViewModel>(content, _jsonOptions);
                return (data, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi GetCategoryById({Id})", id);
                return (null, ex.Message);
            }
        }
    }
}
