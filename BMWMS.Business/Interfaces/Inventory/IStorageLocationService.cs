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

        // --- HIERARCHICAL STRUCTURE & METRICS ---
        Task<List<WarehouseZoneDto>> GetZonesAsync(long warehouseId);

        Task<List<StorageRackDto>> GetRacksAsync(long warehouseId, long? zoneId = null);

        Task<WarehouseStructureDto> GetWarehouseStructureAsync(long warehouseId);

        // --- LOT & INVENTORY DETAILS ---
        Task<LocationDetailWithInventoryDto?> GetLocationInventoryAsync(long locationId);

        // --- PRODUCT LOCATION LOOKUP (SCR-11 / FT-03) ---
        Task<List<ProductLocationSearchItemDto>> SearchProductLocationsAsync(long warehouseId, string keyword);

        // --- ZONE & RACK CREATION ---
        Task<(bool Success, string Message)> CreateZoneAsync(CreateUpdateZoneDto dto);

        Task<(bool Success, string Message)> UpdateZoneAsync(CreateUpdateZoneDto dto);

        Task<(bool Success, string Message)> CreateRackAsync(CreateUpdateRackDto dto);

        Task<(bool Success, string Message)> UpdateRackAsync(CreateUpdateRackDto dto);
    }
}
