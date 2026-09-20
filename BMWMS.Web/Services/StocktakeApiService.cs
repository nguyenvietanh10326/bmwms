using System.Net.Http.Json;
using System.Text.Json;
using BMWMS.Web.Models;

namespace BMWMS.Web.Services
{
    public class StocktakeApiService : IStocktakeApiService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public StocktakeApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
        }

        public async Task<List<StocktakeLocationOptionModel>> GetLocationsAsync(long warehouseId, List<long>? rackIds = null, List<long>? productGroupIds = null)
        {
            try
            {
                var url = $"api/stocktakes/locations?warehouseId={warehouseId}";
                if (rackIds != null && rackIds.Any())
                {
                    url += "&" + string.Join("&", rackIds.Select(id => $"rackIds={id}"));
                }
                if (productGroupIds != null && productGroupIds.Any())
                {
                    url += "&" + string.Join("&", productGroupIds.Select(id => $"productGroupIds={id}"));
                }

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new();
                return await response.Content.ReadFromJsonAsync<List<StocktakeLocationOptionModel>>(JsonOptions) ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<List<StocktakeStaffOptionModel>> GetStaffUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/stocktakes/staff-users");
                if (!response.IsSuccessStatusCode) return new();
                return await response.Content.ReadFromJsonAsync<List<StocktakeStaffOptionModel>>(JsonOptions) ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<List<StocktakeProductLotOptionModel>> SearchProductLotsAsync(string? keyword, int take = 20)
        {
            try
            {
                var url = $"api/stocktakes/product-lots?take={take}";
                if (!string.IsNullOrWhiteSpace(keyword))
                    url += $"&keyword={Uri.EscapeDataString(keyword.Trim())}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new();
                return await response.Content.ReadFromJsonAsync<List<StocktakeProductLotOptionModel>>(JsonOptions) ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<StocktakeSessionPagedResultModel> GetSessionsAsync(StocktakeFilterModel filter)
        {
            try
            {
                var query = $"api/stocktakes?pageIndex={filter.PageIndex}&pageSize={filter.PageSize}";
                if (!string.IsNullOrWhiteSpace(filter.Keyword))
                    query += $"&keyword={Uri.EscapeDataString(filter.Keyword.Trim())}";
                if (!string.IsNullOrWhiteSpace(filter.Status))
                    query += $"&status={Uri.EscapeDataString(filter.Status.Trim())}";
                if (filter.WarehouseId.HasValue && filter.WarehouseId.Value > 0)
                    query += $"&warehouseId={filter.WarehouseId.Value}";
                if (filter.FromDate.HasValue)
                    query += $"&fromDate={filter.FromDate.Value:yyyy-MM-dd}";
                if (filter.ToDate.HasValue)
                    query += $"&toDate={filter.ToDate.Value:yyyy-MM-dd}";

                var response = await _httpClient.GetAsync(query);
                if (!response.IsSuccessStatusCode) return new();
                return await response.Content.ReadFromJsonAsync<StocktakeSessionPagedResultModel>(JsonOptions) ?? new();
            }
            catch
            {
                return new();
            }
        }

        public async Task<StocktakeSessionDetailModel?> GetSessionByIdAsync(long id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/stocktakes/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[StocktakeApi] GetSessionById failed: {response.StatusCode} - {error}");
                    return null;
                }
                return await response.Content.ReadFromJsonAsync<StocktakeSessionDetailModel>(JsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StocktakeApi] GetSessionById exception: {ex.Message}");
                return null;
            }
        }

        public async Task<StocktakeCountTaskModel?> GetCountTaskAsync(long id, long locationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/stocktakes/{id}/locations/{locationId}/count-task");
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[StocktakeApi] GetCountTask failed: {response.StatusCode} - {error}");
                    return null;
                }
                return await response.Content.ReadFromJsonAsync<StocktakeCountTaskModel>(JsonOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StocktakeApi] GetCountTask exception: {ex.Message}");
                return null;
            }
        }

        public async Task<StocktakeActionResultModel> CreateSessionAsync(CreateStocktakeSessionModel request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/stocktakes", request);
                return await ReadActionResultAsync(response);
            }
            catch (Exception ex)
            {
                return Failure($"Loi ket noi: {ex.Message}");
            }
        }

        public async Task<StocktakeActionResultModel> StartSessionAsync(long id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/stocktakes/{id}/start", null);
                return await ReadActionResultAsync(response);
            }
            catch (Exception ex)
            {
                return Failure($"Loi ket noi: {ex.Message}");
            }
        }

        public async Task<StocktakeActionResultModel> CancelSessionAsync(long id, string? notes)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/stocktakes/{id}/cancel", new StocktakeNoteModel { Notes = notes });
                return await ReadActionResultAsync(response);
            }
            catch (Exception ex)
            {
                return Failure($"Loi ket noi: {ex.Message}");
            }
        }

        public async Task<StocktakeActionResultModel> SaveCountsAsync(long id, long locationId, List<StocktakeCountLineModel> lines)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/stocktakes/{id}/locations/{locationId}/counts", lines);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[StocktakeApi] SaveCounts failed: {response.StatusCode} - {error}");
                }
                return await ReadActionResultAsync(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StocktakeApi] SaveCounts exception: {ex.Message}");
                return Failure($"Loi ket noi: {ex.Message}");
            }
        }

        public async Task<StocktakeActionResultModel> SubmitSessionAsync(long id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/stocktakes/{id}/submit", null);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[StocktakeApi] SubmitSession failed: {response.StatusCode} - {error}");
                }
                return await ReadActionResultAsync(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StocktakeApi] SubmitSession exception: {ex.Message}");
                return Failure($"Loi ket noi: {ex.Message}");
            }
        }

        public async Task<StocktakeActionResultModel> SaveSessionCountsAsync(long id, SaveStocktakeSessionCountsModel request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/stocktakes/{id}/counts", request);
                return await ReadActionResultAsync(response);
            }
            catch (Exception ex)
            {
                return Failure($"Lỗi kết nối: {ex.Message}");
            }
        }

        public async Task<StocktakeActionResultModel> AddUnexpectedItemAsync(long id, UnexpectedStocktakeItemModel request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/stocktakes/{id}/unexpected-items", request);
                return await ReadActionResultAsync(response);
            }
            catch (Exception ex)
            {
                return Failure($"Loi ket noi: {ex.Message}");
            }
        }

        public async Task<StocktakeActionResultModel> ApproveSessionAsync(long id, StocktakeNoteModel request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"api/stocktakes/{id}/approve",
                    request);
                return await ReadActionResultAsync(response);
            }
            catch (Exception ex)
            {
                return Failure($"Loi ket noi: {ex.Message}");
            }
        }

        public async Task<StocktakeActionResultModel> RejectSessionAsync(long id, string reason)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    $"api/stocktakes/{id}/reject",
                    new StocktakeNoteModel { Notes = reason });
                return await ReadActionResultAsync(response);
            }
            catch (Exception ex)
            {
                return Failure($"Loi ket noi: {ex.Message}");
            }
        }

        private static async Task<StocktakeActionResultModel> ReadActionResultAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<StocktakeActionResultModel>(JsonOptions)
                    ?? Failure("Không đọc được phản hồi từ hệ thống.");
            }

            var body = await response.Content.ReadAsStringAsync();
            return Failure(ParseMessage(body));
        }

        private static string ParseMessage(string body)
        {
            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(body);
                if (json.TryGetProperty("message", out var message))
                    return message.GetString() ?? body;
                if (json.TryGetProperty("detail", out var detail))
                    return detail.GetString() ?? body;
                if (json.TryGetProperty("title", out var title))
                    return title.GetString() ?? body;
            }
            catch
            {
            }

            return string.IsNullOrWhiteSpace(body) ? "Hệ thống trả về lỗi không xác định." : body;
        }

        private static StocktakeActionResultModel Failure(string message)
        {
            return new StocktakeActionResultModel
            {
                Success = false,
                Message = message
            };
        }
    }
}
