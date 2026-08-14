using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Repository.Interfaces.Inventory
{
    public interface IInventoryRepository
    {
        // 1. Hàm tính 4 con số tổng quan cho Header
        Task<(decimal TotalOnHand, decimal TotalAvailable, decimal TotalReserved, decimal TotalInTransit)> GetInventorySummaryMetricsAsync(
            string? keyword,
            long? warehouseId,
            long? storageLocationId,
            string? status);

        // 2. Hàm lấy danh sách Tồn kho có phân trang
        Task<(List<BMWMS.Repository.Models.Inventory> Items, int TotalCount)> GetPagedInventoryAsync(
            string? keyword,
            long? warehouseId,
            long? storageLocationId,
            string? status,
            int pageIndex,
            int pageSize);

        // 3. Hàm lấy chi tiết 1 dòng tồn kho
        Task<BMWMS.Repository.Models.Inventory?> GetByIdAsync(long inventoryId);

        Task<decimal> GetAvailableQuantityAsync(long productId);

        Task<bool> ReserveStockAsync(long productId, decimal quantity);
    }
}
