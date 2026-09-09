using System;
using System.Collections.Generic;
using System.Linq;

namespace BMWMS.Web.Models.Inventory
{
    public class StorageLocationFilterDto
    {
        public long WarehouseId { get; set; } = 1;
        public string? Keyword { get; set; }
        public string? LocationType { get; set; }
        public string? Status { get; set; }
        public long? ZoneId { get; set; }
        public long? RackId { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class WarehouseHeaderInfoDto
    {
        public long WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int TotalZones { get; set; }
        public int TotalRacks { get; set; }
        public int TotalLocations { get; set; }
    }

    public class StorageLocationItemDto
    {
        public long StorageLocationId { get; set; }
        public long WarehouseId { get; set; }
        public long? RackId { get; set; }
        public long? ZoneId { get; set; }

        public string LocationCode { get; set; } = string.Empty; 
        public string LocationName { get; set; } = string.Empty; 
        public string LocationType { get; set; } = "Bin";        

        public string ZoneCode { get; set; } = "N/A";
        public string ZoneName { get; set; } = "N/A";
        public string RackCode { get; set; } = "N/A";
        public string RackName { get; set; } = "N/A";

        public int Floor { get; set; } = 1;                      
        public decimal? AreaSquareMeter { get; set; }            
        public string DisplayCapacity => AreaSquareMeter.HasValue ? $"{AreaSquareMeter.Value:N0} m²" : "—";

        public decimal? MaxWeightKg { get; set; }
        public decimal? MaxVolumeM3 { get; set; }
        public decimal? CurrentWeightKg { get; set; }
        public decimal? CurrentVolumeM3 { get; set; }
        public string WeightCapacityStatus { get; set; } = "NOT_CONFIGURED";
        public string VolumeCapacityStatus { get; set; } = "NOT_CONFIGURED";
        public string CapacityStatus { get; set; } = "NOT_CONFIGURED";
        public bool HasMissingCapacityData { get; set; }
        public bool IsPutawayAllowed { get; set; } = true;
        public bool IsPickable { get; set; } = true;

        public string Status { get; set; } = "Active";

        // Metrics for visual hierarchy
        public int StoredProductCount { get; set; }
        public int StoredLotCount { get; set; }
        public decimal TotalOnHandQuantity { get; set; }
        public string OccupancyStatus => Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase) || Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase)
            ? "Blocked"
            : CapacityStatus.Equals("EXCEEDED", StringComparison.OrdinalIgnoreCase) ||
              (MaxWeightKg.HasValue && CurrentWeightKg.HasValue && CurrentWeightKg >= MaxWeightKg) ||
              (MaxVolumeM3.HasValue && CurrentVolumeM3.HasValue && CurrentVolumeM3 >= MaxVolumeM3)
                ? "Full"
                : HasMissingCapacityData ? "Unknown"
                : TotalOnHandQuantity > 0 ? "Occupied" : "Available";
    }

    public class CreateUpdateStorageLocationDto
    {
        public long StorageLocationId { get; set; } 
        public long WarehouseId { get; set; }
        public long? RackId { get; set; }

        public string LocationCode { get; set; } = string.Empty;
        public string? LocationName { get; set; }
        public string LocationType { get; set; } = "Bin";

        public decimal? AreaSquareMeter { get; set; }
        public decimal? MaxWeightKg { get; set; }
        public decimal? MaxVolumeM3 { get; set; }

        public bool IsPutawayAllowed { get; set; } = true;
        public bool IsPickable { get; set; } = true;
        public string Status { get; set; } = "Active";
    }

    public class StorageLocationPageDto
    {
        public WarehouseHeaderInfoDto WarehouseInfo { get; set; } = new();
        public List<StorageLocationItemDto> Items { get; set; } = new();

        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 10));
    }

    // --- ZONE DTOs ---
    public class WarehouseZoneDto
    {
        public long ZoneId { get; set; }
        public long WarehouseId { get; set; }
        public string ZoneCode { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? AreaSquareMeter { get; set; }
        public decimal? MaxWeightKg { get; set; }
        public decimal? MaxVolumeM3 { get; set; }
        public decimal AllocatedRackAreaSquareMeter { get; set; }
        public decimal AllocatedRackWeightKg { get; set; }
        public decimal AllocatedRackVolumeM3 { get; set; }
        public string Status { get; set; } = "Active";
        public int RackCount { get; set; }
        public int LocationCount { get; set; }
    }

    public class CreateUpdateZoneDto
    {
        public long ZoneId { get; set; }
        public long WarehouseId { get; set; }
        public string ZoneCode { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? AreaSquareMeter { get; set; }
        public decimal? MaxWeightKg { get; set; }
        public decimal? MaxVolumeM3 { get; set; }
        public string Status { get; set; } = "Active";
    }

    // --- RACK DTOs ---
    public class StorageRackDto
    {
        public long RackId { get; set; }
        public long WarehouseId { get; set; }
        public long ZoneId { get; set; }
        public string ZoneCode { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public string RackCode { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
        public decimal? AreaSquareMeter { get; set; }
        public decimal? MaxWeightKg { get; set; }
        public decimal? MaxVolumeM3 { get; set; }
        public decimal AllocatedLocationAreaSquareMeter { get; set; }
        public decimal AllocatedLocationWeightKg { get; set; }
        public decimal AllocatedLocationVolumeM3 { get; set; }
        public string Status { get; set; } = "Active";
        public int LocationCount { get; set; }
    }

    public class CreateUpdateRackDto
    {
        public long RackId { get; set; }
        public long WarehouseId { get; set; }
        public long ZoneId { get; set; }
        public string RackCode { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
        public decimal? AreaSquareMeter { get; set; }
        public decimal? MaxWeightKg { get; set; }
        public decimal? MaxVolumeM3 { get; set; }
        public string Status { get; set; } = "Active";
    }

    // --- HIERARCHICAL STRUCTURE TREE DTOs ---
    public class WarehouseStructureDto
    {
        public WarehouseHeaderInfoDto WarehouseInfo { get; set; } = new();
        public List<WarehouseZoneStructureDto> Zones { get; set; } = new();
        public int TotalAvailableBins { get; set; }
        public int TotalOccupiedBins { get; set; }
        public int TotalFullBins { get; set; }
        public int TotalUnknownBins { get; set; }
        public int TotalBlockedBins { get; set; }
    }

    public class WarehouseZoneStructureDto
    {
        public long ZoneId { get; set; }
        public string ZoneCode { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? AreaSquareMeter { get; set; }
        public decimal? MaxWeightKg { get; set; }
        public decimal? MaxVolumeM3 { get; set; }
        public decimal AllocatedRackAreaSquareMeter { get; set; }
        public decimal AllocatedRackWeightKg { get; set; }
        public decimal AllocatedRackVolumeM3 { get; set; }
        public string Status { get; set; } = "Active";
        public List<StorageRackStructureDto> Racks { get; set; } = new();
    }

    public class StorageRackStructureDto
    {
        public long RackId { get; set; }
        public long ZoneId { get; set; }
        public string RackCode { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
        public decimal? AreaSquareMeter { get; set; }
        public decimal? MaxWeightKg { get; set; }
        public decimal? MaxVolumeM3 { get; set; }
        public decimal AllocatedLocationAreaSquareMeter { get; set; }
        public decimal AllocatedLocationWeightKg { get; set; }
        public decimal AllocatedLocationVolumeM3 { get; set; }
        public string Status { get; set; } = "Active";
        public List<StorageLocationItemDto> Locations { get; set; } = new();
    }

    // --- LOT & INVENTORY DETAILS AT LOCATION ---
    public class LocationInventoryItemDto
    {
        public long InventoryId { get; set; }
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;
        public string ProductGroupName { get; set; } = string.Empty;

        public long ProductLotId { get; set; }
        public string LotNumber { get; set; } = string.Empty;
        public DateOnly? ManufactureDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public string LotStatus { get; set; } = string.Empty;

        public decimal OnHandQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        public DateTime LastUpdatedAt { get; set; }
    }

    public class LocationDetailWithInventoryDto
    {
        public StorageLocationItemDto Location { get; set; } = new();
        public List<LocationInventoryItemDto> Inventories { get; set; } = new();
    }

    // --- PRODUCT LOCATION SEARCH (SCR-11 / FT-03) ---
    public class ProductLocationSearchItemDto
    {
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;

        public long ProductLotId { get; set; }
        public string LotNumber { get; set; } = string.Empty;
        public DateOnly? ExpiryDate { get; set; }

        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string ZoneCode { get; set; } = string.Empty;
        public string RackCode { get; set; } = string.Empty;
        public long StorageLocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationType { get; set; } = string.Empty;

        public decimal OnHandQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
    }
}
