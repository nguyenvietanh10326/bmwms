using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BMWMS.Business.DTOs.StockOperations;

namespace BMWMS.Business.Interfaces.StockOperations
{
    public interface ITransferOrderService
    {
        Task<TransferOrderPagedResultDto> GetPagedOrdersAsync(string? keyword, string? status, long? warehouseId, int pageIndex, int pageSize, long? currentStaffId);
        Task<TransferOrderDetailViewDto?> GetOrderDetailAsync(long transferOrderId, long currentUserId, bool isManager);
        Task<TransferResultDto> CreateOrderAsync(long staffId, CreateTransferOrderDto dto);
        Task<TransferResultDto> UpdateDraftOrderAsync(long staffId, UpdateTransferOrderDto dto);
    }

    public interface ITransferApprovalService
    {
        Task<TransferResultDto> ApproveTransferAsync(long managerId, ApproveTransferDto dto);
        Task<TransferResultDto> CancelTransferAsync(long actorId, long transferOrderId, string? notes, bool isManager);
    }

    public interface ITransferConfirmService
    {
        Task<TransferResultDto> ConfirmAsync(long staffId, long transferOrderId, ConfirmTransferDto dto);
    }

    public interface ITransferLookupService
    {
        Task<List<ZoneOptionDto>> GetZonesAsync(long warehouseId = 1, long? productId = null);
        Task<List<RackOptionDto>> GetRacksAsync(long warehouseId, long? zoneId = null, long? productId = null);
        Task<List<LocationOptionDto>> GetLocationsAsync(long warehouseId = 1, long? zoneId = null, long? rackId = null, long? productId = null);
        Task<List<TransferInventoryItemDto>> GetLocationInventoryAsync(long locationId);
        Task<BinCapacityCheckDto> ValidateDestinationAsync(long locationId, long productId, decimal requestedQuantity);
        Task<List<StaffOptionDto>> GetStaffUsersAsync();
        Task<List<TransferUseCaseDto>> GetUseCasesAsync();
    }
}
