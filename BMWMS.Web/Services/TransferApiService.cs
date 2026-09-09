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
            public string SourceLocationSummary { get; set; } = "";
            public string DestinationLocationSummary { get; set; } = "";
            public string Status { get; set; } = "";
            public string StatusLabel { get; set; } = "";
            public string StatusCss { get; set; } = "";
            public string ProgressLabel { get; set; } = "";
            public string NextAction { get; set; } = "";
            public string CreatedByName { get; set; } = "";
            public string? AssignedToName { get; set; }
            public string? ConfirmedByName { get; set; }
            public string? DueDateDisplay => DueDate.HasValue ? DueDate.Value.ToString("dd/MM/yyyy") : null;
            public DateOnly RequestedDate { get; set; }
            public DateOnly? DueDate { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ConfirmedAt { get; set; }
            public int TotalItems { get; set; }
            public decimal TotalRequestedQuantity { get; set; }
            public decimal TotalMovedQuantity { get; set; }
            public bool InventoryPosted { get; set; }
            public string? Notes { get; set; }
        }

        public class TransferOrderPagedResultDto
        {
            public List<TransferOrderListDto> Items { get; set; } = new();
            public int TotalCount { get; set; }
            public int PageIndex { get; set; }
            public int PageSize { get; set; }
            public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
            public int DraftCount { get; set; }
            public int ApprovedCount { get; set; }
            public int InProgressCount { get; set; }
            public int CompletedCount { get; set; }
            public int CancelledCount { get; set; }
            public int PendingCount => DraftCount;
            public int RejectedCount => CancelledCount;
            public int IssuedCount => InProgressCount;
            public int ReceivedCount => CompletedCount;
        }

        public class TransferOrderDetailViewDto
        {
            public long TransferOrderId { get; set; }
            public string TransferOrderNumber { get; set; } = "";
            public string TransferType { get; set; } = "";
            public string Status { get; set; } = "";
            public string StatusLabel { get; set; } = "";
            public string ProgressLabel { get; set; } = "";
            public string NextAction { get; set; } = "";
            public string WarehouseName { get; set; } = "";
            public DateOnly RequestedDate { get; set; }
            public DateOnly? DueDate { get; set; }
            public string? Notes { get; set; }
            public string CreatedByName { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public string? AssignedToName { get; set; }
            public long? AssignedToUserId { get; set; }
            public string? ConfirmedByName { get; set; }
            public DateTime? ConfirmedAt { get; set; }
            public bool InventoryPosted { get; set; }
            public bool CanEdit { get; set; }
            public bool CanApprove { get; set; }
            public bool CanReject { get; set; }
            public bool CanIssue { get; set; }
            public bool CanReceive { get; set; }
            public decimal TotalRequestedQuantity { get; set; }
            public decimal TotalMovedQuantity { get; set; }
            public List<TransferOrderDetailItemDto> Details { get; set; } = new();
        }

        public class TransferOrderDetailItemDto
        {
            public long TransferOrderDetailId { get; set; }
            public long ProductId { get; set; }
            public long ProductLotId { get; set; }
            public string ProductCode { get; set; } = "";
            public string ProductName { get; set; } = "";
            public string UnitName { get; set; } = "";
            public byte QuantityScale { get; set; }
            public string LotNumber { get; set; } = "";
            public long SourceLocationId { get; set; }
            public long? SourceZoneId { get; set; }
            public long? SourceRackId { get; set; }
            public string SourceZoneCode { get; set; } = "";
            public string SourceRackCode { get; set; } = "";
            public string SourceLocationCode { get; set; } = "";
            public long DestLocationId { get; set; }
            public long? DestZoneId { get; set; }
            public long? DestRackId { get; set; }
            public string DestZoneCode { get; set; } = "";
            public string DestRackCode { get; set; } = "";
            public string DestLocationCode { get; set; } = "";
            public decimal RequestedQuantity { get; set; }
            public decimal MovedQuantity { get; set; }
            public string SourcePath => string.IsNullOrEmpty(SourceZoneCode)
                ? SourceLocationCode
                : $"{SourceZoneCode} / {SourceRackCode} / {SourceLocationCode}";
            public string DestPath => string.IsNullOrEmpty(DestZoneCode)
                ? DestLocationCode
                : $"{DestZoneCode} / {DestRackCode} / {DestLocationCode}";
        }

        public class CreateTransferItemDto
        {
            public long SourceLocationId { get; set; }
            public long DestLocationId { get; set; }
            public long ProductId { get; set; }
            public long ProductLotId { get; set; }
            public decimal Quantity { get; set; }
        }

        public class CreateTransferOrderDto
        {
            public long WarehouseId { get; set; } = 1;
            public long? AssignedToUserId { get; set; }
            public string? DueDate { get; set; }   // "yyyy-MM-dd" string for JSON
            public string? Notes { get; set; }
            public List<CreateTransferItemDto> Items { get; set; } = new();
        }

        public class UpdateTransferOrderDto : CreateTransferOrderDto
        {
            public long TransferOrderId { get; set; }
        }

        public class ApproveTransferDto
        {
            public long TransferOrderId { get; set; }
            public long? AssignedToUserId { get; set; }
            public string? Notes { get; set; }
        }

        public class ConfirmTransferDto
        {
            public string? Notes { get; set; }
            public List<ConfirmTransferItemDto> Items { get; set; } = new();
            public bool AcknowledgeCapacityWarning { get; set; }
            public string? CapacityWarningReason { get; set; }
            public string? DestinationChangeReason { get; set; }
        }

        public class ConfirmTransferItemDto
        {
            public long TransferOrderDetailId { get; set; }
            public decimal ActualMovedQuantity { get; set; }
            public long? DestinationLocationId { get; set; }
        }

        public class ZoneOptionDto
        {
            public long ZoneId { get; set; }
            public string ZoneCode { get; set; } = "";
            public string ZoneName { get; set; } = "";
        }

        public class RackOptionDto
        {
            public long RackId { get; set; }
            public string RackCode { get; set; } = "";
            public string RackName { get; set; } = "";
            public long ZoneId { get; set; }
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
            public long? ZoneId { get; set; }
            public string ZoneCode { get; set; } = "";
            public long? RackId { get; set; }
            public string RackCode { get; set; } = "";
            public bool IsPutawayAllowed { get; set; }
            public bool IsPickable { get; set; }
            public string Status { get; set; } = "";
            public string DisplayLabel => string.IsNullOrEmpty(ZoneCode) ? LocationCode : $"{ZoneCode} / {RackCode} / {LocationCode}";
        }

        public class BinCapacityCheckDto
        {
            public bool IsValid { get; set; }
            public string Message { get; set; } = "";
        }

        public class StaffOptionDto
        {
            public long UserId { get; set; }
            public string FullName { get; set; } = "";
            public string Username { get; set; } = "";
            public string RoleCode { get; set; } = "";
            public string RoleName { get; set; } = "";
        }

        public class TransferResultDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public long? TransferOrderId { get; set; }
            public string? TransferOrderNumber { get; set; }
        }

        // ── API CALLS ──────────────────────────────────────────────────────────

        public async Task<List<ZoneOptionDto>> GetZonesAsync(long warehouseId = 1)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/transfers/zones?warehouseId={warehouseId}");
                if (!response.IsSuccessStatusCode) return new();
                return await response.Content.ReadFromJsonAsync<List<ZoneOptionDto>>(_jsonOpts) ?? new();
            }
            catch { return new(); }
        }

        public async Task<List<RackOptionDto>> GetRacksAsync(long warehouseId = 1, long? zoneId = null)
        {
            try
            {
                var url = $"api/transfers/racks?warehouseId={warehouseId}";
                if (zoneId.HasValue && zoneId.Value > 0) url += $"&zoneId={zoneId.Value}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new();
                return await response.Content.ReadFromJsonAsync<List<RackOptionDto>>(_jsonOpts) ?? new();
            }
            catch { return new(); }
        }

        public async Task<List<LocationOptionDto>> GetLocationsAsync(long warehouseId = 1, long? zoneId = null, long? rackId = null)
        {
            try
            {
                var url = $"api/transfers/locations?warehouseId={warehouseId}";
                if (zoneId.HasValue && zoneId.Value > 0) url += $"&zoneId={zoneId.Value}";
                if (rackId.HasValue && rackId.Value > 0) url += $"&rackId={rackId.Value}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new();
                return await response.Content.ReadFromJsonAsync<List<LocationOptionDto>>(_jsonOpts) ?? new();
            }
            catch { return new(); }
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

        public async Task<List<StaffOptionDto>> GetStaffUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/transfers/staff-users");
                if (!response.IsSuccessStatusCode) return new();
                return await response.Content.ReadFromJsonAsync<List<StaffOptionDto>>(_jsonOpts) ?? new();
            }
            catch { return new(); }
        }

        public async Task<TransferOrderPagedResultDto> GetOrdersAsync(TransferOrderFilterDto filter)
        {
            try
            {
                var query = $"api/transfers?pageIndex={filter.PageIndex}&pageSize={filter.PageSize}";
                if (!string.IsNullOrWhiteSpace(filter.Keyword)) query += $"&keyword={Uri.EscapeDataString(filter.Keyword.Trim())}";
                if (!string.IsNullOrWhiteSpace(filter.Status)) query += $"&status={Uri.EscapeDataString(filter.Status.Trim())}";
                if (filter.WarehouseId.HasValue && filter.WarehouseId > 0) query += $"&warehouseId={filter.WarehouseId.Value}";
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
                return ParseErrorMessage(await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex) { return new TransferResultDto { Success = false, Message = $"Lỗi kết nối: {ex.Message}" }; }
        }

        public async Task<TransferResultDto> UpdateDraftOrderAsync(long id, UpdateTransferOrderDto request)
        {
            try
            {
                request.TransferOrderId = id;
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"api/transfers/{id}", content);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<TransferResultDto>(_jsonOpts)
                        ?? new TransferResultDto { Success = false, Message = "Loi doc phan hoi." };
                return ParseErrorMessage(await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex) { return new TransferResultDto { Success = false, Message = $"Loi ket noi: {ex.Message}" }; }
        }

        public async Task<TransferResultDto> ApproveOrderAsync(long id, long? assignedToUserId, string? notes)
        {
            try
            {
                var json = JsonSerializer.Serialize(new ApproveTransferDto { TransferOrderId = id, AssignedToUserId = assignedToUserId, Notes = notes });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"api/transfers/{id}/approve", content);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<TransferResultDto>(_jsonOpts)
                        ?? new TransferResultDto { Success = false, Message = "Lỗi đọc phản hồi." };
                return ParseErrorMessage(await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex) { return new TransferResultDto { Success = false, Message = $"Lỗi kết nối: {ex.Message}" }; }
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
                return ParseErrorMessage(await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex) { return new TransferResultDto { Success = false, Message = $"Lỗi kết nối: {ex.Message}" }; }
        }

        public async Task<TransferResultDto> ConfirmTransferAsync(long id, string? notes)
        {
            try
            {
                var json = JsonSerializer.Serialize(new ConfirmTransferDto { Notes = notes });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"api/transfers/{id}/confirm", content);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<TransferResultDto>(_jsonOpts)
                        ?? new TransferResultDto { Success = false, Message = "Lỗi đọc phản hồi." };
                return ParseErrorMessage(await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex) { return new TransferResultDto { Success = false, Message = $"Lỗi kết nối: {ex.Message}" }; }
        }

        public async Task<TransferResultDto> ConfirmIssueAsync(long id, ConfirmTransferDto request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"api/transfers/{id}/issue", content);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<TransferResultDto>(_jsonOpts)
                        ?? new TransferResultDto { Success = false, Message = "Loi doc phan hoi." };
                return ParseErrorMessage(await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex) { return new TransferResultDto { Success = false, Message = $"Loi ket noi: {ex.Message}" }; }
        }

        public async Task<TransferResultDto> ConfirmReceiptAsync(long id, ConfirmTransferDto request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"api/transfers/{id}/receive", content);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<TransferResultDto>(_jsonOpts)
                        ?? new TransferResultDto { Success = false, Message = "Loi doc phan hoi." };
                return ParseErrorMessage(await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex) { return new TransferResultDto { Success = false, Message = $"Loi ket noi: {ex.Message}" }; }
        }

        private static TransferResultDto ParseErrorMessage(string errBody)
        {
            try
            {
                var errObj = JsonSerializer.Deserialize<JsonElement>(errBody);
                var msg = errObj.TryGetProperty("message", out var m) ? m.GetString() : errBody;
                return new TransferResultDto { Success = false, Message = msg ?? "Lỗi không xác định." };
            }
            catch { return new TransferResultDto { Success = false, Message = errBody }; }
        }
    }
}
