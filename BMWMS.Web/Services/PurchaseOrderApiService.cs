using BMWMS.Web.Models.Inventory;
using BMWMS.Web.Models.Warehouse;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace BMWMS.Web.Services
{
    public class PurchaseOrderApiService
    {
        private readonly HttpClient _httpClient;

        public PurchaseOrderApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
        }

        public async Task<(bool IsSuccess, string? Message, string? PoNumber)> CreatePurchaseOrderAsync(PurchaseOrderCreateRequestModel model)
        {
            var response = await _httpClient.PostAsJsonAsync("api/PurchaseOrders", model);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                string poNumber = string.Empty;
                try {
                    var json = JsonNode.Parse(content);
                    poNumber = json?["data"]?.ToString() ?? string.Empty;
                } catch { }
                return (true, null, poNumber);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            string errorMessage = "Lỗi tạo Purchase Order";
            try {
                var json = JsonNode.Parse(errorContent);
                errorMessage = json?["message"]?.ToString() ?? json?["title"]?.ToString() ?? errorMessage;
                if (json?["detail"] != null) {
                    errorMessage += " - " + json["detail"]?.ToString();
                }
            } catch { }
            return (false, errorMessage, null);
        }

        public async Task<PagedResultDto<PurchaseOrderListDto>?> GetPagedPurchaseOrdersAsync(PurchaseOrderFilterDto filter)
        {
            var queryString = $"?SearchTerm={filter.SearchTerm}&Status={filter.Status}&PageIndex={filter.PageIndex}&PageSize={filter.PageSize}";
            return await _httpClient.GetFromJsonAsync<PagedResultDto<PurchaseOrderListDto>>($"api/PurchaseOrders{queryString}");
        }

        public async Task<PurchaseOrderDetailDto?> GetPurchaseOrderByIdAsync(long id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<PurchaseOrderDetailDto>($"api/PurchaseOrders/{id}");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool IsSuccess, string? Message)> ConfirmPurchaseOrderAsync(long id)
        {
            try 
            {
                var response = await _httpClient.PostAsync($"api/PurchaseOrders/{id}/confirm", null);
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Xác nhận thành công");
                }
                var errorContent = await response.Content.ReadAsStringAsync();
                string errorMessage = "Lỗi xác nhận Purchase Order";
                try {
                    var json = JsonNode.Parse(errorContent);
                    errorMessage = json?["message"]?.ToString() ?? json?["title"]?.ToString() ?? errorMessage;
                } catch { }
                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối hoặc timeout: {ex.Message}");
            }
        }

        public async Task<(bool IsSuccess, string? Message)> SendPurchaseOrderToSupplierAsync(long id)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/PurchaseOrders/{id}/send-to-supplier", null);
                var content = await response.Content.ReadAsStringAsync();
                var fallback = response.IsSuccessStatusCode
                    ? "Đã gửi email PO cho nhà cung cấp."
                    : "Gửi email PO cho nhà cung cấp thất bại.";
                try
                {
                    var json = JsonNode.Parse(content);
                    fallback = json?["message"]?.ToString() ?? json?["title"]?.ToString() ?? fallback;
                }
                catch { }
                return (response.IsSuccessStatusCode, fallback);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối hoặc timeout khi gửi email: {ex.Message}");
            }
        }

        public async Task<(bool IsSuccess, string? Message)> CancelPurchaseOrderAsync(long id, string? reason = null)
        {
            try
            {
                var queryString = string.IsNullOrWhiteSpace(reason) ? "" : $"?reason={Uri.EscapeDataString(reason)}";
                var response = await _httpClient.PostAsync($"api/PurchaseOrders/{id}/cancel{queryString}", null);
                if (response.IsSuccessStatusCode)
                {
                    return (true, "Hủy thành công");
                }
                var errorContent = await response.Content.ReadAsStringAsync();
                string errorMessage = "Lỗi hủy Purchase Order";
                try {
                    var json = JsonNode.Parse(errorContent);
                    errorMessage = json?["message"]?.ToString() ?? json?["title"]?.ToString() ?? errorMessage;
                } catch { }
                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối hoặc timeout: {ex.Message}");
            }
        }

        public async Task<(bool IsSuccess, string? Message)> ClosePartiallyReceivedOrderAsync(long id, string reason)
        {
            try
            {
                var response = await _httpClient.PostAsync(
                    $"api/PurchaseOrders/{id}/close-partial?reason={Uri.EscapeDataString(reason ?? string.Empty)}", null);
                var content = await response.Content.ReadAsStringAsync();
                var message = response.IsSuccessStatusCode ? "Đã kết thúc PO." : "Không thể kết thúc PO.";
                try
                {
                    var json = JsonNode.Parse(content);
                    message = json?["message"]?.ToString() ?? message;
                }
                catch { }
                return (response.IsSuccessStatusCode, message);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối hoặc timeout: {ex.Message}");
            }
        }

        public async Task<(bool IsSuccess, string? Message)> ContinuePartiallyReceivedOrderAsync(
            long id,
            DateOnly requestedDeliveryDate,
            string? note)
        {
            try
            {
                var query = $"?requestedDeliveryDate={requestedDeliveryDate:yyyy-MM-dd}";
                if (!string.IsNullOrWhiteSpace(note))
                    query += $"&note={Uri.EscapeDataString(note.Trim())}";
                var response = await _httpClient.PostAsync($"api/PurchaseOrders/{id}/continue-partial{query}", null);
                var content = await response.Content.ReadAsStringAsync();
                var message = response.IsSuccessStatusCode ? "Đã gửi đề nghị giao tiếp." : "Không thể gửi đề nghị giao tiếp.";
                try
                {
                    var json = JsonNode.Parse(content);
                    message = json?["message"]?.ToString() ?? message;
                }
                catch { }
                return (response.IsSuccessStatusCode, message);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối hoặc timeout: {ex.Message}");
            }
        }
    }
}
