using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace BMWMS.Web.Services
{
    public class TransferApiService
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

        public TransferApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
        }

        // ── DTOs ──────────────────────────────────────────────────────────────

        public class TransferOrderFilterDto
        {
            public string? Keyword { get; set; }
            public string? Status { get; set; }
            public long? WarehouseId { get; set; }
            public int PageIndex { get; set; } = 1;
            public int PageSize { get; set; } = 15;
        }

        public class TransferOrderListDto
        {
            public long TransferOrderId { get; set; }
            public string TransferOrderNumber { get; set; } = "";
            public string TransferType { get; set; } = "";
            public string WarehouseName { get; set; } = "";
            public string Status { get; set; } = "";
            public string StatusLabel { get; set; } = "";
            public string StatusCss { get; set; } = "";
            public string CreatedByName { get; set; } = "";
            public string? ConfirmedByName { get; set; }
            public DateOnly RequestedDate { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ConfirmedAt { get; set; }
            public int TotalItems { get; set; }
            public string? Notes { get; set; }
        }

        public class TransferOrderPagedResultDto
        {
            public List<TransferOrderListDto> Items { get; set; } = new();
            public int TotalCount { get; set; }
            public int PageIndex { get; set; }
            public int PageSize { get; set; }
            public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
            public int PendingCount { get; set; }
            public int ApprovedCount { get; set; }
            public int RejectedCount { get; set; }
        }

        public class TransferOrderDetailViewDto
        {
            public long TransferOrderId { get; set; }
            public string TransferOrderNumber { get; set; } = "";
            public string TransferType { get; set; } = "";
            public string Status { get; set; } = "";
            public string StatusLabel { get; set; } = "";
            public string WarehouseName { get; set; } = "";
            public DateOnly RequestedDate { get; set; }
            public string? Notes { get; set; }
            public string CreatedByName { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public string? ConfirmedByName { get; set; }
            public DateTime? ConfirmedAt { get; set; }
            public List<TransferOrderDetailItemDto> Details { get; set; } = new();
        }

        public class TransferOrderDetailItemDto
        {
            public long TransferOrderDetailId { get; set; }
            public string ProductCode { get; set; } = "";
            public string ProductName { get; set; } = "";
            public string UnitName { get; set; } = "";
            public string LotNumber { get; set; } = "";
            public string SourceLocationCode { get; set; } = "";
            public string DestLocationCode { get; set; } = "";
            public decimal RequestedQuantity { get; set; }
            public decimal MovedQuantity { get; set; }
        }

        public class CreateTransferOrderDto
        {
            public long WarehouseId { get; set; }
            public long SourceLocationId { get; set; }
            public long DestLocationId { get; set; }
            public long ProductId { get; set; }
            public long ProductLotId { get; set; }
            public decimal Quantity { get; set; }
            public string? Notes { get; set; }
        }

        public class ApproveTransferDto
        {
            public long TransferOrderId { get; set; }
            public string? Notes { get; set; }
        }

        public class TransferInventoryItemDto
        {
            public long InventoryId { get; set; }
            public long ProductId { get; set; }
            public string ProductCode { get; set; } = "";
            public string ProductName { get; set; } = "";
            public string UnitName { get; set; } = "";
            public long ProductLotId { get; set; }
            public string LotNumber { get; set; } = "";
            public string ExpiryDisplay { get; set; } = "";
            public decimal OnHandQuantity { get; set; }
            public decimal ReservedQuantity { get; set; }
            public decimal AvailableQuantity { get; set; }
        }

        public class LocationOptionDto
        {
            public long LocationId { get; set; }
            public string LocationCode { get; set; } = "";
            public string LocationName { get; set; } = "";
            public string ZoneCode { get; set; } = "";
            public string RackCode { get; set; } = "";
            public bool IsPutawayAllowed { get; set; }
            public string Status { get; set; } = "";
            public string DisplayLabel => string.IsNullOrEmpty(ZoneCode) ? LocationCode : $"{ZoneCode}/{RackCode}/{LocationCode}";
        }

        public class BinCapacityCheckDto
        {
            public bool IsValid { get; set; }
            public string Message { get; set; } = "";
        }

        public class TransferResultDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public long? TransferOrderId { get; set; }
            public string? TransferOrderNumber { get; set; }
        }

        // ── API CALLS ──────────────────────────────────────────────────────────

        public async Task<TransferOrderPagedResultDto> GetOrdersAsync(TransferOrderFilterDto filter)
        {
            try
            {
                var query = $"api/transfers?pageIndex={filter.PageIndex}&pageSize={filter.PageSize}";
                if (!string.IsNullOrWhiteSpace(filter.Keyword))
                    query += $"&keyword={Uri.EscapeDataString(filter.Keyword.Trim())}";
                if (!string.IsNullOrWhiteSpace(filter.Status))
                    query += $"&status={Uri.EscapeDataString(filter.Status.Trim())}";
                if (filter.WarehouseId.HasValue && filter.WarehouseId > 0)
                    query += $"&warehouseId={filter.WarehouseId.Value}";

                var response = await _httpClient.GetAsync(query);
                if (!response.IsSuccessStatusCode) return new();
                return await response.Content.ReadFromJsonAsync<TransferOrderPagedResultDto>(_jsonOpts) ?? new();
            }
            catch { return new(); }
        }

        public async Task<TransferOrderDetailViewDto?> GetOrderByIdAsync(long id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/transfers/{id}");
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<TransferOrderDetailViewDto>(_jsonOpts);
            }
            catch { return null; }
        }

        public async Task<List<TransferInventoryItemDto>> GetLocationInventoryAsync(long locationId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/transfers/location-inventory?locationId={locationId}");
                if (!response.IsSuccessStatusCode) return new();
                return await response.Content.ReadFromJsonAsync<List<TransferInventoryItemDto>>(_jsonOpts) ?? new();
            }
            catch { return new(); }
        }

        public async Task<List<LocationOptionDto>> GetLocationsAsync(long warehouseId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/transfers/locations?warehouseId={warehouseId}");
                if (!response.IsSuccessStatusCode) return new();
                return await response.Content.ReadFromJsonAsync<List<LocationOptionDto>>(_jsonOpts) ?? new();
            }
            catch { return new(); }
        }

        public async Task<BinCapacityCheckDto> ValidateDestinationAsync(long destLocationId, long sourceLocationId, long productId)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"api/transfers/validate-destination?destLocationId={destLocationId}&sourceLocationId={sourceLocationId}&productId={productId}");
                if (!response.IsSuccessStatusCode)
                    return new BinCapacityCheckDto { IsValid = false, Message = "Lỗi kết nối API." };
                return await response.Content.ReadFromJsonAsync<BinCapacityCheckDto>(_jsonOpts)
                    ?? new BinCapacityCheckDto { IsValid = false, Message = "Không đọc được phản hồi." };
            }
            catch { return new BinCapacityCheckDto { IsValid = false, Message = "Lỗi kết nối." }; }
        }

        public async Task<TransferResultDto> CreatePendingOrderAsync(CreateTransferOrderDto request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/transfers/create", content);

                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<TransferResultDto>(_jsonOpts)
                        ?? new TransferResultDto { Success = false, Message = "Lỗi đọc phản hồi." };

                var errBody = await response.Content.ReadAsStringAsync();
                return ParseErrorMessage(errBody);
            }
            catch (Exception ex)
            {
                return new TransferResultDto { Success = false, Message = $"Lỗi kết nối: {ex.Message}" };
            }
        }

        public async Task<TransferResultDto> ApproveOrderAsync(long id, string? notes)
        {
            try
            {
                var json = JsonSerializer.Serialize(new ApproveTransferDto { TransferOrderId = id, Notes = notes });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"api/transfers/{id}/approve", content);

                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<TransferResultDto>(_jsonOpts)
                        ?? new TransferResultDto { Success = false, Message = "Lỗi đọc phản hồi." };

                var errBody = await response.Content.ReadAsStringAsync();
                return ParseErrorMessage(errBody);
            }
            catch (Exception ex)
            {
                return new TransferResultDto { Success = false, Message = $"Lỗi kết nối: {ex.Message}" };
            }
        }

        public async Task<TransferResultDto> RejectOrderAsync(long id, string? notes)
        {
            try
            {
                var json = JsonSerializer.Serialize(new ApproveTransferDto { TransferOrderId = id, Notes = notes });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"api/transfers/{id}/reject", content);

                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<TransferResultDto>(_jsonOpts)
                        ?? new TransferResultDto { Success = false, Message = "Lỗi đọc phản hồi." };

                var errBody = await response.Content.ReadAsStringAsync();
                return ParseErrorMessage(errBody);
            }
            catch (Exception ex)
            {
                return new TransferResultDto { Success = false, Message = $"Lỗi kết nối: {ex.Message}" };
            }
        }

        private static TransferResultDto ParseErrorMessage(string errBody)
        {
            try
            {
                var errObj = JsonSerializer.Deserialize<JsonElement>(errBody);
                var msg = errObj.TryGetProperty("message", out var m) ? m.GetString() : errBody;
                return new TransferResultDto { Success = false, Message = msg ?? "Lỗi không xác định." };
            }
            catch
            {
                return new TransferResultDto { Success = false, Message = errBody };
            }
        }
    }
}
