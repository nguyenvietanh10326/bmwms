using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class WarehouseZone
{
    public long ZoneId { get; set; }

    public long WarehouseId { get; set; }

    public string ZoneCode { get; set; } = null!;

    public string ZoneName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal? AreaSquareMeter { get; set; }

    public decimal? MaxWeightKg { get; set; }

    public decimal? MaxVolumeM3 { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<StorageRack> StorageRacks { get; set; } = new List<StorageRack>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
