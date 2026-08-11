using BMWMS.Business.DTOs.StockOperations;

namespace BMWMS.Business.Interfaces.StockOperations
{
    public interface ITransferService
    {
        Task<List<ZoneOptionDto>> GetZonesAsync(long warehouseId = 1);
        Task<List<RackOptionDto>> GetRacksAsync(long warehouseId = 1, long? zoneId = null);
        Task<List<TransferInventoryItemDto>> GetLocationInventoryAsync(long locationId);
        Task<List<LocationOptionDto>> GetLocationsForDropdownAsync(long warehouseId = 1, long? zoneId = null, long? rackId = null);
        Task<BinCapacityCheckDto> ValidateDestinationAsync(long destLocationId, long sourceLocationId, long productId);
        Task<List<StaffOptionDto>> GetStaffUsersAsync();
        Task<List<TransferUseCaseDto>> GetUseCasesAsync();

        Task<TransferOrderPagedResultDto> GetPagedOrdersAsync(TransferOrderFilterDto filter);
        Task<TransferOrderDetailViewDto?> GetOrderDetailAsync(long transferOrderId);

        Task<TransferResultDto> CreatePendingOrderAsync(CreateTransferOrderDto dto, long createdByUserId);
        Task<TransferResultDto> UpdateDraftOrderAsync(long transferOrderId, UpdateTransferOrderDto dto, long updatedByUserId);
        Task<TransferResultDto> ApproveOrderAsync(long transferOrderId, long approvedByUserId, ApproveTransferDto dto);
        Task<TransferResultDto> RejectOrderAsync(long transferOrderId, long rejectedByUserId, string? notes);

        Task<TransferResultDto> ConfirmTransferIssueAsync(long transferOrderId, long staffUserId, string? notes);
        Task<TransferResultDto> ConfirmTransferReceiptAsync(long transferOrderId, long staffUserId, string? notes);

        // Backward compatible one-shot confirm endpoint for older clients.
        Task<TransferResultDto> ConfirmTransferAsync(long transferOrderId, long staffUserId, string? notes);
    }
}
