using BMWMS.Web.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace BMWMS.Web.Services
{
    public class ProductApiService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public ProductApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
        }

        public async Task<PagedResultModel<ProductResponseModel>> GetPagedListAsync(ProductFilterModel filter)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"pageIndex={filter.PageIndex}",
                    $"pageSize={filter.PageSize}"
                };

                if (!string.IsNullOrWhiteSpace(filter.Keyword))
                    queryParams.Add($"keyword={Uri.EscapeDataString(filter.Keyword.Trim())}");

                if (filter.ProductGroupId.HasValue && filter.ProductGroupId.Value > 0)
                    queryParams.Add($"productGroupId={filter.ProductGroupId.Value}");

                if (filter.UnitOfMeasureId.HasValue && filter.UnitOfMeasureId.Value > 0)
                    queryParams.Add($"unitOfMeasureId={filter.UnitOfMeasureId.Value}");

                if (!string.IsNullOrWhiteSpace(filter.Status))
                    queryParams.Add($"status={Uri.EscapeDataString(filter.Status.Trim())}");

                if (!string.IsNullOrWhiteSpace(filter.RotationMethod))
                    queryParams.Add($"rotationMethod={Uri.EscapeDataString(filter.RotationMethod.Trim())}");

                var url = "api/products?" + string.Join("&", queryParams);
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PagedResultModel<ProductResponseModel>>(_jsonOptions);
                    return result ?? new PagedResultModel<ProductResponseModel>();
                }

                return new PagedResultModel<ProductResponseModel>();
            }
            catch
            {
                return new PagedResultModel<ProductResponseModel>();
            }
        }

        public async Task<ProductDetailResponseModel?> GetByIdAsync(long id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/products/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ProductDetailResponseModel>(_jsonOptions);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<UnitOfMeasureModel>> GetUnitsOfMeasureAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/products/units");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<UnitOfMeasureModel>>(_jsonOptions) ?? new();
                }
                return new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<List<ProductGroupOptionModel>> GetProductGroupsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/products/groups");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<ProductGroupOptionModel>>(_jsonOptions) ?? new();
                }
                return new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<(bool Success, string Message)> CreateAsync(CreateProductModel dto)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/products", dto);
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Tạo sản phẩm mới thành công!");
                }

                var errorObj = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                if (errorObj.TryGetProperty("message", out var msgProp))
                {
                    return (false, msgProp.GetString() ?? "Lỗi khi tạo sản phẩm.");
                }

                return (false, $"Lỗi API: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, $"Không thể kết nối đến máy chủ API: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateAsync(long id, UpdateProductModel dto)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/products/{id}", dto);
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Cập nhật sản phẩm thành công!");
                }

                var errorObj = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                if (errorObj.TryGetProperty("message", out var msgProp))
                {
                    return (false, msgProp.GetString() ?? "Lỗi khi cập nhật sản phẩm.");
                }

                return (false, $"Lỗi API: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, $"Không thể kết nối đến máy chủ API: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> ToggleStatusAsync(long id)
        {
            try
            {
                var response = await _httpClient.PatchAsync($"api/products/{id}/status", null);
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Cập nhật trạng thái thành công!");
                }

                var errorObj = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                if (errorObj.TryGetProperty("message", out var msgProp))
                {
                    return (false, msgProp.GetString() ?? "Lỗi khi thay đổi trạng thái.");
                }

                return (false, $"Lỗi API: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, $"Không thể kết nối đến máy chủ API: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteAsync(long id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/products/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Xóa sản phẩm thành công!");
                }

                var errorObj = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                if (errorObj.TryGetProperty("message", out var msgProp))
                {
                    return (false, msgProp.GetString() ?? "Lỗi khi xóa sản phẩm.");
                }

                return (false, $"Lỗi API: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, $"Không thể kết nối đến máy chủ API: {ex.Message}");
            }
        }
    }
}
