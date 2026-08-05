using BMWMS.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Repository.Interfaces.Inventory
{
    public interface IStorageLocationRepository
    {
        Task<Warehouse?> GetWarehouseByIdAsync(long warehouseId);

        Task<(List<StorageLocation> Items, int TotalCount)> GetPagedLocationsAsync(
            long warehouseId,
            string? keyword,
            string? locationType,
            string? status,
            int pageIndex,
            int pageSize,
            long? zoneId = null,
            long? rackId = null);

        Task<StorageLocation?> GetByIdAsync(long locationId);

        Task<StorageLocation> AddAsync(StorageLocation entity);

        Task UpdateAsync(StorageLocation entity);

        Task<bool> ExistsCodeAsync(long warehouseId, string locationCode, long? excludeId = null);

        // --- HIERARCHY & RELATIONS ---
        Task<List<WarehouseZone>> GetZonesByWarehouseAsync(long warehouseId);

        Task<List<StorageRack>> GetRacksByWarehouseAsync(long warehouseId, long? zoneId = null);

        Task<List<StorageLocation>> GetAllLocationsWithHierarchyAndInventoryAsync(long warehouseId);

        Task<List<BMWMS.Repository.Models.Inventory>> GetLocationInventoryAsync(long locationId);

        Task<List<VwProductLocationLookup>> SearchProductLocationsAsync(long warehouseId, string keyword);

        Task<WarehouseZone> AddZoneAsync(WarehouseZone zone);

        Task<StorageRack> AddRackAsync(StorageRack rack);

        Task<bool> ExistsZoneCodeAsync(long warehouseId, string zoneCode);

        Task<bool> ExistsRackCodeAsync(long warehouseId, string rackCode);
    }
}
