using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Repository.Repositories
{
    public class WarehouseRepository : IWarehouseRepository
    {
        private readonly BmwmsContext _context;

        public WarehouseRepository(BmwmsContext context)
        {
            _context = context;
        }

        public async Task<Warehouse?> GetByIdAsync(long warehouseId)
        {
            return await _context.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.WarehouseId == warehouseId);
        }

        public async Task<Warehouse?> GetByCodeAsync(string warehouseCode)
        {
            return await _context.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.WarehouseCode == warehouseCode);
        }

        public async Task<(List<Warehouse> Items, int TotalCount)> GetPagedListAsync(
            string? keyword,
            string? status,
            bool? isPrimary,
            int pageIndex,
            int pageSize)
        {
            var query = _context.Warehouses.AsNoTracking().AsQueryable();

            // Lọc theo từ khóa (Code hoặc Name)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var trimmedKeyword = keyword.Trim().ToLower();
                query = query.Where(w => w.WarehouseCode.ToLower().Contains(trimmedKeyword)
                                      || w.WarehouseName.ToLower().Contains(trimmedKeyword));
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(w => w.Status == status);
            }

            // Lọc theo kho chính
            if (isPrimary.HasValue)
            {
                query = query.Where(w => w.IsPrimary == isPrimary.Value);
            }

            // Đếm tổng số bản ghi
            var totalCount = await query.CountAsync();

            // Sắp xếp và phân trang
            var items = await query
                .OrderByDescending(w => w.IsPrimary)
                .ThenBy(w => w.WarehouseCode)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<bool> IsCodeExistsAsync(string warehouseCode, long? excludeWarehouseId = null)
        {
            return await _context.Warehouses.AnyAsync(w =>
                w.WarehouseCode == warehouseCode &&
                (!excludeWarehouseId.HasValue || w.WarehouseId != excludeWarehouseId.Value));
        }

        public async Task<bool> HasPrimaryWarehouseAsync(long? excludeWarehouseId = null)
        {
            return await _context.Warehouses.AnyAsync(w =>
                w.IsPrimary &&
                (!excludeWarehouseId.HasValue || w.WarehouseId != excludeWarehouseId.Value));
        }

        public async Task<long> AddAsync(Warehouse warehouse)
        {
            await _context.Warehouses.AddAsync(warehouse);
            await _context.SaveChangesAsync();
            return warehouse.WarehouseId;
        }

        public async Task UpdateAsync(Warehouse warehouse)
        {
            _context.Warehouses.Update(warehouse);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long warehouseId)
        {
            var warehouse = await _context.Warehouses.FindAsync(warehouseId);
            if (warehouse != null)
            {
                _context.Warehouses.Remove(warehouse);
                await _context.SaveChangesAsync();
            }
        }
    }
}
