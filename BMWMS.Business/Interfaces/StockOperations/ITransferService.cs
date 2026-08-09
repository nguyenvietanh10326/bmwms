using BMWMS.Business.DTOs.StockOperations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.Business.Interfaces.StockOperations
{
    public interface ITransferService
    {
        // FORM DATA
        Task<List<TransferInventoryItemDto>> GetLocationInventoryAsync(long locationId);
        Task<List<LocationOptionDto>> GetLocationsForDropdownAsync(long warehouseId);
        Task<BinCapacityCheckDto> ValidateDestinationAsync(long destLocationId, long sourceLocationId, long productId);

        // LIST
        Task<TransferOrderPagedResultDto> GetPagedOrdersAsync(TransferOrderFilterDto filter);

        // DETAIL
        Task<TransferOrderDetailViewDto?> GetOrderDetailAsync(long transferOrderId);

        // STAFF: Tạo phiếu PENDING
        Task<TransferResultDto> CreatePendingOrderAsync(CreateTransferOrderDto dto, long createdByUserId);

        // MANAGER: Duyệt
        Task<TransferResultDto> ApproveOrderAsync(long transferOrderId, long approvedByUserId, string? notes);

        // MANAGER: Từ chối
        Task<TransferResultDto> RejectOrderAsync(long transferOrderId, long rejectedByUserId, string? notes);
    }
}
