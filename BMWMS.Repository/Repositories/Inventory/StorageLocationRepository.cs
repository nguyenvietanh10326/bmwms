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

        // 2. Query Danh sách có Include StorageRack & WarehouseZone + lọc Zone/Rack
        public async Task<(List<StorageLocation> Items, int TotalCount)> GetPagedLocationsAsync(
            long warehouseId,
            string? keyword,
            string? locationType,
            string? status,
            int pageIndex,
            int pageSize,
            long? zoneId = null,
            long? rackId = null)
        {
            var query = _context.StorageLocations
                .AsNoTracking()
                .Include(l => l.StorageRack)
                    .ThenInclude(r => r!.WarehouseZone)
                .Include(l => l.Inventories)
                .Where(l => l.WarehouseId == warehouseId)
                .AsQueryable();

            if (zoneId.HasValue && zoneId.Value > 0)
            {
                query = query.Where(l => l.StorageRack != null && l.StorageRack.ZoneId == zoneId.Value);
            }

            if (rackId.HasValue && rackId.Value > 0)
            {
                query = query.Where(l => l.RackId == rackId.Value);
            }

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
                .Include(l => l.Inventories)
                    .ThenInclude(inv => inv.Product)
                .Include(l => l.Inventories)
                    .ThenInclude(inv => inv.ProductLot)
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

        // --- HIERARCHY & RELATIONS IMPLEMENTATION ---
        public async Task<List<WarehouseZone>> GetZonesByWarehouseAsync(long warehouseId)
        {
            return await _context.WarehouseZones
                .AsNoTracking()
                .Include(z => z.StorageRacks)
                    .ThenInclude(r => r.StorageLocations)
                .Where(z => z.WarehouseId == warehouseId)
                .OrderBy(z => z.ZoneCode)
                .ToListAsync();
        }

        public async Task<List<StorageRack>> GetRacksByWarehouseAsync(long warehouseId, long? zoneId = null)
        {
            var query = _context.StorageRacks
                .AsNoTracking()
                .Include(r => r.WarehouseZone)
                .Include(r => r.StorageLocations)
                .Where(r => r.WarehouseId == warehouseId);

            if (zoneId.HasValue && zoneId.Value > 0)
            {
                query = query.Where(r => r.ZoneId == zoneId.Value);
            }

            return await query
                .OrderBy(r => r.RackCode)
                .ToListAsync();
        }

        public async Task<List<StorageLocation>> GetAllLocationsWithHierarchyAndInventoryAsync(long warehouseId)
        {
            return await _context.StorageLocations
                .AsNoTracking()
                .Include(l => l.StorageRack)
                    .ThenInclude(r => r!.WarehouseZone)
                .Include(l => l.Inventories)
                    .ThenInclude(inv => inv.Product)
                .Include(l => l.Inventories)
                    .ThenInclude(inv => inv.ProductLot)
                .Where(l => l.WarehouseId == warehouseId)
                .OrderBy(l => l.LocationCode)
                .ToListAsync();
        }

        public async Task<List<BMWMS.Repository.Models.Inventory>> GetLocationInventoryAsync(long locationId)
        {
            return await _context.Inventories
                .AsNoTracking()
                .Include(inv => inv.Product)
                    .ThenInclude(p => p.ProductGroup)
                .Include(inv => inv.Product)
                    .ThenInclude(p => p.UnitOfMeasure)
                .Include(inv => inv.ProductLot)
                .Where(inv => inv.StorageLocationId == locationId && inv.OnHandQuantity > 0)
                .OrderBy(inv => inv.Product.ProductName)
                .ToListAsync();
        }

        public async Task<List<VwProductLocationLookup>> SearchProductLocationsAsync(long warehouseId, string keyword)
        {
            var trimmed = keyword.Trim().ToLower();
            return await _context.VwProductLocationLookups
                .AsNoTracking()
                .Where(v => v.WarehouseId == warehouseId &&
                           (v.ProductCode.ToLower().Contains(trimmed) ||
                            v.ProductName.ToLower().Contains(trimmed) ||
                            v.LotNumber.ToLower().Contains(trimmed) ||
                            v.LocationCode.ToLower().Contains(trimmed)))
                .OrderBy(v => v.ProductName)
                .ThenBy(v => v.LocationCode)
                .ToListAsync();
        }

        public async Task<WarehouseZone> AddZoneAsync(WarehouseZone zone)
        {
            await _context.WarehouseZones.AddAsync(zone);
            await _context.SaveChangesAsync();
            return zone;
        }

        public async Task<WarehouseZone?> GetZoneByIdAsync(long zoneId)
        {
            return await _context.WarehouseZones
                .Include(zone => zone.StorageRacks)
                    .ThenInclude(rack => rack.StorageLocations)
                .FirstOrDefaultAsync(zone => zone.ZoneId == zoneId);
        }

        public async Task UpdateZoneAsync(WarehouseZone zone)
        {
            await _context.SaveChangesAsync();
        }

        public async Task<StorageRack> AddRackAsync(StorageRack rack)
        {
            await _context.StorageRacks.AddAsync(rack);
            await _context.SaveChangesAsync();
            return rack;
        }

        public async Task<StorageRack?> GetRackByIdAsync(long rackId)
        {
            return await _context.StorageRacks
                .Include(rack => rack.StorageLocations)
                .Include(rack => rack.WarehouseZone)
                    .ThenInclude(zone => zone.StorageRacks)
                .FirstOrDefaultAsync(rack => rack.RackId == rackId);
        }

        public async Task UpdateRackAsync(StorageRack rack)
        {
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsZoneCodeAsync(long warehouseId, string zoneCode, long? excludeId = null)
        {
            var codeUpper = zoneCode.Trim().ToUpper();
            return await _context.WarehouseZones
                .AnyAsync(z => z.WarehouseId == warehouseId && z.ZoneCode.ToUpper() == codeUpper &&
                               (!excludeId.HasValue || z.ZoneId != excludeId.Value));
        }

        public async Task<bool> ExistsRackCodeAsync(long warehouseId, string rackCode, long? excludeId = null)
        {
            var codeUpper = rackCode.Trim().ToUpper();
            return await _context.StorageRacks
                .AnyAsync(r => r.WarehouseId == warehouseId && r.RackCode.ToUpper() == codeUpper &&
                               (!excludeId.HasValue || r.RackId != excludeId.Value));
        }

        public async Task<List<string>> GetLocationDeactivationBlockersAsync(long locationId)
        {
            var reasons = new List<string>();

            if (await _context.Inventories.AnyAsync(inventory =>
                    inventory.StorageLocationId == locationId &&
                    (inventory.OnHandQuantity > 0 || inventory.ReservedQuantity > 0)))
                reasons.Add("vẫn còn tồn kho hoặc số lượng đã giữ chỗ");

            if (await _context.ProductFixedLocations.AnyAsync(mapping => mapping.StorageLocationId == locationId))
                reasons.Add("đang được cấu hình làm vị trí ưu tiên cho sản phẩm");

            if (await _context.TransferOrderDetails.AnyAsync(detail =>
                    (detail.SourceLocationId == locationId || detail.DestinationLocationId == locationId) &&
                    detail.TransferOrder.Status != "COMPLETED" &&
                    detail.TransferOrder.Status != "CANCELLED"))
                reasons.Add("đang thuộc lệnh điều chuyển chưa hoàn tất");

            if (await _context.StocktakeItems.AnyAsync(item =>
                    item.StorageLocationId == locationId &&
                    item.StocktakeSession.Status != "COMPLETED" &&
                    item.StocktakeSession.Status != "APPROVED" &&
                    item.StocktakeSession.Status != "CANCELLED"))
                reasons.Add("đang thuộc phiên kiểm kê chưa kết thúc");

            return reasons;
        }
    }
}
