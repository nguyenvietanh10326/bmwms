using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Repository.Repositories.Inventory
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly BmwmsContext _context; 

        public InventoryRepository(BmwmsContext context)
        {
            _context = context;
        }

        private IQueryable<BMWMS.Repository.Models.Inventory> BuildFilterQuery(
            string? keyword,
            long? warehouseId,
            long? storageLocationId,
            string? status)
        {
            var query = _context.Inventories
                .AsNoTracking()
                .Include(i => i.Product)
                    .ThenInclude(p => p.UnitOfMeasure)
                .Include(i => i.ProductLot)
                .Include(i => i.StorageLocation)
                    .ThenInclude(sl => sl.Warehouse)
                .AsQueryable();

            // 1. Tìm theo Mã hoặc Tên sản phẩm
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var trimmed = keyword.Trim().ToLower();
                query = query.Where(i => i.Product.ProductCode.ToLower().Contains(trimmed)
                                      || i.Product.ProductName.ToLower().Contains(trimmed)
                                      || i.ProductLot.LotNumber.ToLower().Contains(trimmed));
            }

            // 2. Lọc theo Kho
            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                query = query.Where(i => i.StorageLocation.WarehouseId == warehouseId.Value);
            }

            // 3. Lọc theo Vị trí 
            if (storageLocationId.HasValue && storageLocationId.Value > 0)
            {
                query = query.Where(i => i.StorageLocationId == storageLocationId.Value);
            }

            // 4. Lọc theo Trạng thái 
            if (!string.IsNullOrWhiteSpace(status) && status.ToUpper() != "ALL")
            {
                var upperStatus = status.Trim().ToUpper();
                if (upperStatus == "OUT_OF_STOCK") 
                {
                    query = query.Where(i => (i.AvailableQuantity ?? (i.OnHandQuantity - i.ReservedQuantity)) <= 0);
                }
                else if (upperStatus == "LOW_STOCK") 
                {

                    query = query.Where(i => (i.AvailableQuantity ?? (i.OnHandQuantity - i.ReservedQuantity)) > 0
                                          && (i.AvailableQuantity ?? (i.OnHandQuantity - i.ReservedQuantity)) < 300);
                }
                else if (upperStatus == "NORMAL") 
                {
                    query = query.Where(i => (i.AvailableQuantity ?? (i.OnHandQuantity - i.ReservedQuantity)) >= 300);
                }
            }

            return query;
        }

        public async Task<(decimal TotalOnHand, decimal TotalAvailable, decimal TotalReserved, decimal TotalInTransit)> GetInventorySummaryMetricsAsync(
            string? keyword,
            long? warehouseId,
            long? storageLocationId,
            string? status)
        {
            var query = BuildFilterQuery(keyword, warehouseId, storageLocationId, status);

            var totalOnHand = await query.SumAsync(i => (decimal?)i.OnHandQuantity) ?? 0m;
            var totalReserved = await query.SumAsync(i => (decimal?)i.ReservedQuantity) ?? 0m;
            var totalAvailable = await query.SumAsync(i => (decimal?)(i.AvailableQuantity ?? (i.OnHandQuantity - i.ReservedQuantity))) ?? 0m;

            // Giả định InTransit tính từ đơn chuyển kho hoặc tạm tính 0 nếu chưa có logic riêng
            decimal totalInTransit = 720m; // Có thể query từ bảng TransferOrder nếu có

            return (totalOnHand, totalAvailable, totalReserved, totalInTransit);
        }

        // 2. Lấy danh sách tồn kho có phân trang
        public async Task<(List<BMWMS.Repository.Models.Inventory> Items, int TotalCount)> GetPagedInventoryAsync(
            string? keyword,
            long? warehouseId,
            long? storageLocationId,
            string? status,
            int pageIndex,
            int pageSize)
        {
            var query = BuildFilterQuery(keyword, warehouseId, storageLocationId, status);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(i => i.LastUpdatedAt)
                .ThenBy(i => i.Product.ProductCode)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        // 3. Lấy chi tiết dòng tồn kho
        public async Task<BMWMS.Repository.Models.Inventory?> GetByIdAsync(long inventoryId)
        {
            return await _context.Inventories
                .AsNoTracking()
                .Include(i => i.Product)
                    .ThenInclude(p => p.UnitOfMeasure)
                .Include(i => i.ProductLot)
                .Include(i => i.StorageLocation)
                    .ThenInclude(sl => sl.Warehouse)
                .FirstOrDefaultAsync(i => i.InventoryId == inventoryId);
        }

        public async Task<decimal> GetAvailableQuantityAsync(long productId)
        {
            return await _context.Inventories
                .Where(x => x.ProductId == productId)
                .SumAsync(x => x.OnHandQuantity - x.ReservedQuantity);
        }
        public async Task<bool> ReserveStockAsync(
    long productId,
    decimal quantity)
        {
            if (quantity <= 0)
                return false;

            var inventories = await _context.Inventories
                .Where(x => x.ProductId == productId)
                .OrderBy(x => x.InventoryId)
                .ToListAsync();

            decimal totalAvailable = inventories.Sum(
                x => x.OnHandQuantity - x.ReservedQuantity);

            // Không đủ tồn
            if (totalAvailable < quantity)
                return false;

            decimal remaining = quantity;

            foreach (var inventory in inventories)
            {
                if (remaining <= 0)
                    break;

                decimal available =
                    inventory.OnHandQuantity -
                    inventory.ReservedQuantity;

                if (available <= 0)
                    continue;

                decimal reserveQuantity =
                    Math.Min(available, remaining);

                inventory.ReservedQuantity += reserveQuantity;

                remaining -= reserveQuantity;
            }

            await _context.SaveChangesAsync();

            return remaining == 0;
        }

    }
}
