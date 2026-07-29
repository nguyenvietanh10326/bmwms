using BMWMS.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Repository.Interfaces
{
    public interface IWarehouseRepository
    {
        Task<Warehouse?> GetByIdAsync(long warehouseId);
        Task<Warehouse?> GetByCodeAsync(string warehouseCode);

        Task<(List<Warehouse> Items, int TotalCount)> GetPagedListAsync(
            string? keyword,
            string? status,
            bool? isPrimary,
            int pageIndex,
            int pageSize);

        Task<bool> IsCodeExistsAsync(string warehouseCode, long? excludeWarehouseId = null);
        Task<bool> HasPrimaryWarehouseAsync(long? excludeWarehouseId = null);

        Task<long> AddAsync(Warehouse warehouse);
        Task UpdateAsync(Warehouse warehouse);
        Task DeleteAsync(long warehouseId);
    }
}
