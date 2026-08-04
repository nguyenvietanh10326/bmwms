using BMWMS.Web.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace BMWMS.Web.Services
{
    public class ProductGroupApiService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public ProductGroupApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
        }

        public async Task<PagedResultModel<ProductGroupViewModel>> GetPagedListAsync(string? keyword, string? status, int pageIndex = 1, int pageSize = 20)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"pageIndex={pageIndex}",
                    $"pageSize={pageSize}"
                };

                if (!string.IsNullOrWhiteSpace(keyword))
                    queryParams.Add($"keyword={Uri.EscapeDataString(keyword.Trim())}");

                if (!string.IsNullOrWhiteSpace(status))
                    queryParams.Add($"status={Uri.EscapeDataString(status.Trim())}");

                var url = "api/product-groups?" + string.Join("&", queryParams);
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PagedResultModel<ProductGroupViewModel>>(_jsonOptions);
                    return result ?? new PagedResultModel<ProductGroupViewModel>();
                }

                return new PagedResultModel<ProductGroupViewModel>();
            }
            catch
            {
                return new PagedResultModel<ProductGroupViewModel>();
            }
        }

        public async Task<List<ProductGroupViewModel>> GetAllActiveAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/product-groups/active");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<ProductGroupViewModel>>(_jsonOptions) ?? new();
                }
                return new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<ProductGroupDetailViewModel?> GetByIdAsync(long id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/product-groups/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ProductGroupDetailViewModel>(_jsonOptions);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<GroupAttributeConfigViewModel>> GetGroupAttributesAsync(long id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/product-groups/{id}/attributes");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<GroupAttributeConfigViewModel>>(_jsonOptions) ?? new();
                }
                return new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<List<ProductAttributeViewModel>> GetAllAttributesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/product-groups/attributes/all");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<ProductAttributeViewModel>>(_jsonOptions) ?? new();
                }
                return new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<(bool Success, string Message, long? Id)> CreateAsync(CreateProductGroupInputModel model)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/product-groups", model);
                if (response.IsSuccessStatusCode)
                {
                    var resObj = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                    long? id = null;
                    if (resObj.TryGetProperty("id", out var idProp))
                    {
                        id = idProp.GetInt64();
                    }
                    return (true, "Tạo nhóm sản phẩm thành công!", id);
                }

                var errorObj = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                if (errorObj.TryGetProperty("message", out var msgProp))
                {
                    return (false, msgProp.GetString() ?? "Lỗi khi tạo nhóm sản phẩm.", null);
                }

                return (false, $"Lỗi API: {response.StatusCode}", null);
            }
            catch (Exception ex)
            {
                return (false, $"Không thể kết nối đến máy chủ API: {ex.Message}", null);
            }
        }

        public async Task<(bool Success, string Message)> UpdateAsync(long id, UpdateProductGroupInputModel model)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/product-groups/{id}", model);
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Cập nhật nhóm sản phẩm thành công!");
                }

                var errorObj = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                if (errorObj.TryGetProperty("message", out var msgProp))
                {
                    return (false, msgProp.GetString() ?? "Lỗi khi cập nhật nhóm sản phẩm.");
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
                var response = await _httpClient.PatchAsync($"api/product-groups/{id}/status", null);
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Thay đổi trạng thái thành công!");
                }

                var errorObj = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                if (errorObj.TryGetProperty("message", out var msgProp))
                {
                    return (false, msgProp.GetString() ?? "Lỗi khi đổi trạng thái.");
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
                var response = await _httpClient.DeleteAsync($"api/product-groups/{id}");
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Xóa nhóm sản phẩm thành công!");
                }

                var errorObj = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                if (errorObj.TryGetProperty("message", out var msgProp))
                {
                    return (false, msgProp.GetString() ?? "Lỗi khi xóa nhóm sản phẩm.");
                }

                return (false, $"Lỗi API: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return (false, $"Không thể kết nối đến máy chủ API: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateGroupAttributesAsync(long id, List<GroupAttributeAssignmentInputModel> attributes)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/product-groups/{id}/attributes", attributes);
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Cập nhật cấu hình thuộc tính thành công!");
                }

                var errorObj = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
                if (errorObj.TryGetProperty("message", out var msgProp))
                {
                    return (false, msgProp.GetString() ?? "Lỗi khi lưu cấu hình thuộc tính.");
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
