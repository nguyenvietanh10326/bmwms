using System.Text;
using System.Text.Json;

namespace BMWMS.Web.Services
{
    public class TransferApiService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public TransferApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ApiClient");
        }

        #region DTOs
        public class TransferOrderPagedResultDto
        {
            public List<TransferOrderDto> Items { get; set; } = new();
            public int TotalCount { get; set; }
            public int PageIndex { get; set; }
            public int PageSize { get; set; }
            public int DraftCount { get; set; }
            public int CompletedCount { get; set; }
            public int CancelledCount { get; set; }
            public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
        }

        public class TransferOrderDto
        {
            public long TransferOrderId { get; set; }
            public string TransferOrderNumber { get; set; } = "";
            public string TransferType { get; set; } = "";
            public string Status { get; set; } = "";
            public string StatusLabel { get; set; } = "";
            public string StatusCss { get; set; } = "";
            public string WarehouseName { get; set; } = "";
            public string SourceLocationSummary { get; set; } = "";
            public string DestinationLocationSummary { get; set; } = "";
            public int TotalItems { get; set; }
            public decimal TotalRequestedQuantity { get; set; }
            public decimal TotalMovedQuantity { get; set; }
            public string CreatedByName { get; set; } = "";
            public string? ApprovedByName { get; set; }
            public string? AssignedToName { get; set; }
            public string? ConfirmedByName { get; set; }
            public DateOnly RequestedDate { get; set; }
            public DateOnly? DueDate { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? ApprovedAt { get; set; }
            public DateTime? ConfirmedAt { get; set; }
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
            public DateOnly? DueDate { get; set; }
            public string? Notes { get; set; }
            public string CreatedByName { get; set; } = "";
            public DateTime CreatedAt { get; set; }
            public string? ApprovedByName { get; set; }
            public DateTime? ApprovedAt { get; set; }
            public string? AssignedToName { get; set; }
            public string? ConfirmedByName { get; set; }
            public DateTime? ConfirmedAt { get; set; }
            public bool CanEdit { get; set; }
            public bool CanCancel { get; set; }
            public bool CanConfirm { get; set; }
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
            public string LotNumber { get; set; } = "";
            public DateOnly? FirstReceivedDate { get; set; }
            public DateOnly? ExpiryDate { get; set; }
            public long SourceLocationId { get; set; }
            public long? SourceZoneId { get; set; }
            public long? SourceRackId { get; set; }
            public string SourceLocationCode { get; set; } = "";
            public long DestLocationId { get; set; }
            public long? DestZoneId { get; set; }
            public long? DestRackId { get; set; }
            public string DestLocationCode { get; set; } = "";
            public decimal RequestedQuantity { get; set; }
            public decimal MovedQuantity { get; set; }
            public byte QuantityScale { get; set; }
        }

        public class CreateTransferOrderDto
        {
            public long WarehouseId { get; set; }
            public DateOnly? DueDate { get; set; }
            public string? Notes { get; set; }
            public List<CreateTransferItemDto> Items { get; set; } = new();
        }

        public class CreateTransferItemDto
        {
            public long ProductId { get; set; }
            public long ProductLotId { get; set; }
            public long SourceLocationId { get; set; }
            public long DestLocationId { get; set; }
            public decimal Quantity { get; set; }
        }

        public class UpdateTransferOrderDto : CreateTransferOrderDto
        {
            public long TransferOrderId { get; set; }
        }

        public class CancelTransferDto
        {
            public string? Notes { get; set; }
        }

        public class ConfirmTransferDto
        {
            public List<ConfirmTransferItemDto> Items { get; set; } = new();
            public bool AcknowledgeCapacityWarning { get; set; }
            public string? CapacityWarningReason { get; set; }
            public string? DestinationChangeReason { get; set; }
            public string? ShortfallReason { get; set; }
            public string? Notes { get; set; }
        }

        public class ConfirmTransferItemDto
        {
            public long TransferOrderDetailId { get; set; }
            public decimal ActualMovedQuantity { get; set; }
            public long? DestinationLocationId { get; set; }
        }

        public class TransferResultDto
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public long? TransferOrderId { get; set; }
            public string? TransferOrderNumber { get; set; }
        }

        public class TransferOrderFilterDto
        {
            public string? Keyword { get; set; }
            public string? Status { get; set; }
            public long? WarehouseId { get; set; }
            public int PageIndex { get; set; } = 1;
            public int PageSize { get; set; } = 15;
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
            public string DisplayLabel => string.IsNullOrWhiteSpace(LocationName) ? LocationCode : $"{LocationCode} - {LocationName}";
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
            public DateOnly? FirstReceivedDate { get; set; }
            public DateOnly? ExpiryDate { get; set; }
            public decimal OnHandQuantity { get; set; }
            public decimal ReservedQuantity { get; set; }
            public decimal AvailableQuantity { get; set; }
            public byte QuantityScale { get; set; }
        }

        public class BinCapacityCheckDto
        {
            public bool IsValid { get; set; }
            public string CapacityStatus { get; set; } = "";
            public string Message { get; set; } = "";
            public decimal? CurrentQuantity { get; set; }
            public decimal? ProjectedQuantity { get; set; }
            public decimal? MaxCapacityQuantity { get; set; }
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
            public long? ZoneId { get; set; }
        }
        #endregion

        #region API Methods
        public async Task<TransferOrderPagedResultDto> GetOrdersAsync(TransferOrderFilterDto filter)
        {
            try
            {
                var query = $"api/transfers?pageIndex={filter.PageIndex}&pageSize={filter.PageSize}";
                if (!string.IsNullOrWhiteSpace(filter.Keyword)) query += $"&keyword={Uri.EscapeDataString(filter.Keyword.Trim())}";
                if (!string.IsNullOrWhiteSpace(filter.Status)) query += $"&status={Uri.EscapeDataString(filter.Status.Trim())}";
                if (filter.WarehouseId.HasValue && filter.WarehouseId > 0) query += $"&warehouseId={filter.WarehouseId.Value}";
                
                var response = await _httpClient.GetAsync(query);
                if (!response.IsSuccessStatusCode) return new TransferOrderPagedResultDto();
                return await response.Content.ReadFromJsonAsync<TransferOrderPagedResultDto>(_jsonOpts) ?? new TransferOrderPagedResultDto();
            }
            catch { return new TransferOrderPagedResultDto(); }
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
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/transfers/create", content);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<TransferResultDto>(_jsonOpts) ?? new TransferResultDto { Success = false };
                return new TransferResultDto { Success = false, Message = await response.Content.ReadAsStringAsync() };
            }
            catch (Exception ex) { return new TransferResultDto { Success = false, Message = ex.Message }; }
        }

        public async Task<TransferResultDto> UpdateDraftOrderAsync(long id, UpdateTransferOrderDto request)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"api/transfers/{id}", content);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<TransferResultDto>(_jsonOpts) ?? new TransferResultDto { Success = false };
                return new TransferResultDto { Success = false, Message = await response.Content.ReadAsStringAsync() };
            }
            catch (Exception ex) { return new TransferResultDto { Success = false, Message = ex.Message }; }
        }

        public async Task<TransferResultDto> CancelOrderAsync(long id, string? notes)
        {
            try
            {
                var dto = new CancelTransferDto { Notes = notes };
                var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"api/transfers/{id}/cancel", content);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<TransferResultDto>(_jsonOpts) ?? new TransferResultDto { Success = false };
                return new TransferResultDto { Success = false, Message = await response.Content.ReadAsStringAsync() };
            }
            catch (Exception ex) { return new TransferResultDto { Success = false, Message = ex.Message }; }
        }

        public async Task<TransferResultDto> ConfirmTransferAsync(long id, ConfirmTransferDto request)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"api/transfers/{id}/confirm", content);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<TransferResultDto>(_jsonOpts) ?? new TransferResultDto { Success = false };
                return new TransferResultDto { Success = false, Message = await response.Content.ReadAsStringAsync() };
            }
            catch (Exception ex) { return new TransferResultDto { Success = false, Message = ex.Message }; }
        }

        public async Task<List<LocationOptionDto>> GetLocationsAsync(long warehouseId = 1, long? zoneId = null, long? rackId = null, long? productId = null)
        {
            try
            {
                var url = $"api/transfers/locations?warehouseId={warehouseId}";
                if (zoneId.HasValue && zoneId.Value > 0) url += $"&zoneId={zoneId.Value}";
                if (rackId.HasValue && rackId.Value > 0) url += $"&rackId={rackId.Value}";
                if (productId.HasValue && productId.Value > 0) url += $"&productId={productId.Value}";
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
        
        public async Task<List<ZoneOptionDto>> GetZonesAsync(long warehouseId = 1, long? productId = null)
        {
            try
            {
                var url = $"api/transfers/zones?warehouseId={warehouseId}";
                if (productId.HasValue && productId.Value > 0) url += $"&productId={productId.Value}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new();
                return await response.Content.ReadFromJsonAsync<List<ZoneOptionDto>>(_jsonOpts) ?? new();
            }
            catch { return new(); }
        }

        public async Task<List<RackOptionDto>> GetRacksAsync(long warehouseId = 1, long? zoneId = null, long? productId = null)
        {
            try
            {
                var url = $"api/transfers/racks?warehouseId={warehouseId}";
                if (zoneId.HasValue && zoneId.Value > 0) url += $"&zoneId={zoneId.Value}";
                if (productId.HasValue && productId.Value > 0) url += $"&productId={productId.Value}";
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new();
                return await response.Content.ReadFromJsonAsync<List<RackOptionDto>>(_jsonOpts) ?? new();
            }
            catch { return new(); }
        }

        public async Task<BinCapacityCheckDto> ValidateDestinationAsync(long locationId, long productId, decimal requestedQuantity)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/transfers/validate-destination?locationId={locationId}&productId={productId}&requestedQuantity={requestedQuantity.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                if (!response.IsSuccessStatusCode) return new BinCapacityCheckDto { IsValid = false, Message = await response.Content.ReadAsStringAsync() };
                return await response.Content.ReadFromJsonAsync<BinCapacityCheckDto>(_jsonOpts) ?? new BinCapacityCheckDto { IsValid = false, Message = "Khong doc duoc ket qua kiem tra suc chua." };
            }
            catch (Exception ex) { return new BinCapacityCheckDto { IsValid = false, Message = ex.Message }; }
        }
        #endregion
    }
}
