using BMWMS.Business.DTOs.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Business.Interfaces.Inventory
{
    public interface IStorageLocationService
    {
        Task<StorageLocationPageDto> GetLocationsPageAsync(StorageLocationFilterDto filter);

        Task<StorageLocationItemDto?> GetByIdAsync(long locationId);

        Task<(bool Success, string Message)> CreateLocationAsync(CreateUpdateStorageLocationDto dto);

        Task<(bool Success, string Message)> UpdateLocationAsync(CreateUpdateStorageLocationDto dto);
    }
}
