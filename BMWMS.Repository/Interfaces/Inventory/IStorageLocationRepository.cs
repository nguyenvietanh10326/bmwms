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
            int pageSize);

        Task<StorageLocation?> GetByIdAsync(long locationId);

        Task<StorageLocation> AddAsync(StorageLocation entity);

        Task UpdateAsync(StorageLocation entity);

        Task<bool> ExistsCodeAsync(long warehouseId, string locationCode, long? excludeId = null);
    }
}
