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
    public class StorageLocationRepository : IStorageLocationRepository
    {
        private readonly BmwmsContext _context;

        public StorageLocationRepository(BmwmsContext context)
        {
            _context = context;
        }

        // 1. Lấy thông tin Kho
        public async Task<Warehouse?> GetWarehouseByIdAsync(long warehouseId)
        {
            return await _context.Warehouses
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.WarehouseId == warehouseId);
        }

        // 2. Query Danh sách có Include StorageRack & WarehouseZone
        public async Task<(List<StorageLocation> Items, int TotalCount)> GetPagedLocationsAsync(
            long warehouseId,
            string? keyword,
            string? locationType,
            string? status,
            int pageIndex,
            int pageSize)
        {
            var query = _context.StorageLocations
                .AsNoTracking()
                .Include(l => l.StorageRack)
                    .ThenInclude(r => r!.WarehouseZone)
                .Where(l => l.WarehouseId == warehouseId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var trimmed = keyword.Trim().ToLower();
                query = query.Where(l => l.LocationCode.ToLower().Contains(trimmed)
                                      || (l.LocationName != null && l.LocationName.ToLower().Contains(trimmed))
                                      || (l.StorageRack != null && l.StorageRack.RackCode.ToLower().Contains(trimmed))
                                      || (l.StorageRack != null && l.StorageRack.WarehouseZone != null && l.StorageRack.WarehouseZone.ZoneCode.ToLower().Contains(trimmed)));
            }

            if (!string.IsNullOrWhiteSpace(locationType) && locationType.ToUpper() != "ALL")
            {
                query = query.Where(l => l.LocationType.ToLower() == locationType.Trim().ToLower());
            }

            if (!string.IsNullOrWhiteSpace(status) && status.ToUpper() != "ALL")
            {
                query = query.Where(l => l.Status.ToLower() == status.Trim().ToLower());
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(l => l.LocationCode)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<StorageLocation?> GetByIdAsync(long locationId)
        {
            return await _context.StorageLocations
                .AsNoTracking()
                .Include(l => l.Warehouse)
                .Include(l => l.StorageRack)
                    .ThenInclude(r => r!.WarehouseZone)
                .FirstOrDefaultAsync(l => l.StorageLocationId == locationId);
        }

        public async Task<StorageLocation> AddAsync(StorageLocation entity)
        {
            await _context.StorageLocations.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(StorageLocation entity)
        {
            _context.StorageLocations.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsCodeAsync(long warehouseId, string locationCode, long? excludeId = null)
        {
            var codeUpper = locationCode.Trim().ToUpper();
            return await _context.StorageLocations
                .AnyAsync(l => l.WarehouseId == warehouseId
                            && l.LocationCode.ToUpper() == codeUpper
                            && (!excludeId.HasValue || l.StorageLocationId != excludeId.Value));
        }
    
}
}
