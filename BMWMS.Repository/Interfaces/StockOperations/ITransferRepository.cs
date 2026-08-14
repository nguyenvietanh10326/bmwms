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

    public interface ITransferRepository
    {
        Task<List<WarehouseZone>> GetZonesByWarehouseAsync(long warehouseId = 1);
        Task<List<StorageRack>> GetRacksByZoneAsync(long warehouseId, long? zoneId = null);
        Task<List<BMWMS.Repository.Models.Inventory>> GetInventoriesByLocationAsync(long locationId);
        Task<StorageLocation?> GetLocationWithInventoryAsync(long locationId);
        Task<List<StorageLocation>> GetActiveLocationsByWarehouseAsync(long warehouseId = 1, long? zoneId = null, long? rackId = null);
        Task<List<Warehouse>> GetAllWarehousesAsync();
        Task<List<User>> GetStaffUsersAsync();

        Task<(List<TransferOrder> Items, int TotalCount, int DraftCount, int ApprovedCount, int InProgressCount, int CompletedCount, int CancelledCount)>
            GetPagedOrdersAsync(string? keyword, string? status, long? warehouseId, int pageIndex, int pageSize);

        Task<TransferOrder?> GetOrderWithDetailsAsync(long transferOrderId);

        Task<TransferOrder> CreatePendingOrderAsync(
            long warehouseId,
            long? assignedToUserId,
            DateOnly? dueDate,
            List<TransferItemParam> items,
            long createdByUserId,
            string? notes);

        Task<TransferOrder> ApproveOrderAsync(long transferOrderId, long approvedByUserId, long? assignedToUserId, string? notes);
        Task<TransferOrder> UpdateDraftOrderAsync(
            long transferOrderId,
            long warehouseId,
            long? assignedToUserId,
            DateOnly? dueDate,
            List<TransferItemParam> items,
            long updatedByUserId,
            string? notes);
        Task<TransferOrder> RejectOrderAsync(long transferOrderId, long rejectedByUserId, string? notes);
        Task<TransferOrder> ConfirmTransferIssueAsync(long transferOrderId, long staffUserId, string? notes);
        Task<TransferOrder> ConfirmTransferReceiptAsync(long transferOrderId, long staffUserId, string? notes);

        // Backward compatible one-shot confirm endpoint for older clients.
        Task<TransferOrder> ConfirmTransferAsync(long transferOrderId, long staffUserId, string? notes);
    }
}
