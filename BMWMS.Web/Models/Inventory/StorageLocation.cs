using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Web.Models.Inventory
{
    public class StorageLocationFilterDto
    {
        public long WarehouseId { get; set; }
        public string? Keyword { get; set; }
        public string? LocationType { get; set; }
        public string? Status { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class WarehouseHeaderInfoDto
    {
        public long WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class StorageLocationItemDto
    {
        public long StorageLocationId { get; set; }
        public long WarehouseId { get; set; }
        public long? RackId { get; set; }

        public string LocationCode { get; set; } = string.Empty; 
        public string LocationName { get; set; } = string.Empty; 
        public string LocationType { get; set; } = "Bin";        

        public string ZoneCode { get; set; } = "N/A";
        public string RackCode { get; set; } = "N/A";

        public int Floor { get; set; } = 1;                      
        public decimal? AreaSquareMeter { get; set; }            
        public string DisplayCapacity => AreaSquareMeter.HasValue ? $"{AreaSquareMeter.Value:N0} m²" : "—";

        public decimal? MaxWeightKg { get; set; }
        public decimal? MaxVolumeM3 { get; set; }
        public bool IsPutawayAllowed { get; set; } = true;
        public bool IsPickable { get; set; } = true;

        public string Status { get; set; } = "Active";
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
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
