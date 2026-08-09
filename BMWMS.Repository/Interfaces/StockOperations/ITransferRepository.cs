using BMWMS.Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.Repository.Interfaces.StockOperations
{
    public interface ITransferRepository
    {
        // ── FORM DATA ──────────────────────────────────────────────────────────
        Task<List<BMWMS.Repository.Models.Inventory>> GetInventoriesByLocationAsync(long locationId);
        Task<StorageLocation?> GetLocationWithInventoryAsync(long locationId);
        Task<List<StorageLocation>> GetActiveLocationsByWarehouseAsync(long warehouseId);
        Task<List<Warehouse>> GetAllWarehousesAsync();

        // ── LIST / DETAIL ──────────────────────────────────────────────────────
        Task<(List<TransferOrder> Items, int TotalCount, int PendingCount, int ApprovedCount, int RejectedCount)>
            GetPagedOrdersAsync(string? keyword, string? status, long? warehouseId, int pageIndex, int pageSize);

        Task<TransferOrder?> GetOrderWithDetailsAsync(long transferOrderId);

        // ── CREATE (Staff) ─────────────────────────────────────────────────────
        /// <summary>Tạo TransferOrder + Detail với Status = PENDING — chưa chạm Inventory.</summary>
        Task<TransferOrder> CreatePendingOrderAsync(
            long warehouseId,
            long sourceLocationId,
            long destLocationId,
            long productId,
            long productLotId,
            decimal quantity,
            long createdByUserId,
            string? notes);

        // ── APPROVE (Manager) ──────────────────────────────────────────────────
        /// <summary>Duyệt: INSERT 2 InventoryTransactions → Trigger tự cập nhật Inventory.
        /// Cập nhật TransferOrder.Status = COMPLETED.</summary>
        Task<TransferOrder> ApproveOrderAsync(long transferOrderId, long approvedByUserId, string? notes);

        // ── REJECT (Manager) ───────────────────────────────────────────────────
        Task<TransferOrder> RejectOrderAsync(long transferOrderId, long rejectedByUserId, string? notes);
    }
}
