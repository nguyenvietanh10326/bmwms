using BMWMS.Repository.Models;

namespace BMWMS.Repository.Interfaces.StockOperations
{
    public class TransferItemParam
    {
        public long SourceLocationId { get; set; }
        public long DestLocationId { get; set; }
        public long ProductId { get; set; }
        public long ProductLotId { get; set; }
        public decimal Quantity { get; set; }
    }

    public class TransferConfirmItemParam
    {
        public long TransferOrderDetailId { get; set; }
        public decimal ActualMovedQuantity { get; set; }
        public long? DestinationLocationId { get; set; }
    }

    public interface ITransferRepository
    {
        Task<List<WarehouseZone>> GetZonesByWarehouseAsync(long warehouseId = 1, long? productId = null);
        Task<List<StorageRack>> GetRacksByZoneAsync(long warehouseId, long? zoneId = null, long? productId = null);
        Task<List<BMWMS.Repository.Models.Inventory>> GetInventoriesByLocationAsync(long locationId);
        Task<StorageLocation?> GetLocationWithInventoryAsync(long locationId);
        Task<List<StorageLocation>> GetActiveLocationsByWarehouseAsync(long warehouseId = 1, long? zoneId = null, long? rackId = null, long? productId = null);
        Task<List<Warehouse>> GetAllWarehousesAsync();
        Task<List<User>> GetStaffUsersAsync();

        Task<(List<TransferOrder> Items, int TotalCount, int DraftCount, int ApprovedCount, int CompletedCount, int CancelledCount)>
            GetPagedOrdersAsync(string? keyword, string? status, long? warehouseId, int pageIndex, int pageSize, long? currentStaffId);

        Task<TransferOrder?> GetOrderWithDetailsAsync(long transferOrderId);

        Task<IReadOnlyList<string>> ValidateTransferItemsAsync(
            long warehouseId,
            IReadOnlyCollection<TransferItemParam> items);

        Task<TransferOrder> CreatePendingOrderAsync(
            long warehouseId,
            DateOnly? dueDate,
            List<TransferItemParam> items,
            long createdByUserId,
            string? notes);

        Task<TransferOrder> UpdateDraftOrderAsync(
            long transferOrderId,
            long warehouseId,
            DateOnly? dueDate,
            List<TransferItemParam> items,
            long updatedByUserId,
            string? notes);

        Task<TransferOrder> ApproveOrderAsync(long transferOrderId, long approvedByUserId, string? notes);
        Task<TransferOrder> CancelOrderAsync(long transferOrderId, long cancelledByUserId, string? notes);
        
        Task<TransferOrder> ConfirmTransferAsync(
            long transferOrderId, 
            long staffUserId, 
            IReadOnlyCollection<TransferConfirmItemParam> items, 
            string? destinationChangeReason, 
            string? shortfallReason, 
            string? notes);
    }
}
