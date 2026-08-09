using BMWMS.Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        // ── FORM & DROPDOWN DATA ───────────────────────────────────────────────
        Task<List<WarehouseZone>> GetZonesByWarehouseAsync(long warehouseId = 1);
        Task<List<BMWMS.Repository.Models.Inventory>> GetInventoriesByLocationAsync(long locationId);
        Task<StorageLocation?> GetLocationWithInventoryAsync(long locationId);
        Task<List<StorageLocation>> GetActiveLocationsByWarehouseAsync(long warehouseId = 1, long? zoneId = null);
        Task<List<Warehouse>> GetAllWarehousesAsync();

        // ── LIST / DETAIL ──────────────────────────────────────────────────────
        Task<(List<TransferOrder> Items, int TotalCount, int PendingCount, int ApprovedCount, int RejectedCount)>
            GetPagedOrdersAsync(string? keyword, string? status, long? warehouseId, int pageIndex, int pageSize);

        Task<TransferOrder?> GetOrderWithDetailsAsync(long transferOrderId);

        // ── CREATE MULTI-ITEM (Staff) ──────────────────────────────────────────
        Task<TransferOrder> CreatePendingOrderAsync(
            long warehouseId,
            List<TransferItemParam> items,
            long createdByUserId,
            string? notes);

        // ── APPROVE (Manager) ──────────────────────────────────────────────────
        Task<TransferOrder> ApproveOrderAsync(long transferOrderId, long approvedByUserId, string? notes);

        // ── REJECT (Manager) ───────────────────────────────────────────────────
        Task<TransferOrder> RejectOrderAsync(long transferOrderId, long rejectedByUserId, string? notes);
    }
}
