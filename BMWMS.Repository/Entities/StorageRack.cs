using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class StorageRack
{
    public long RackId { get; set; }

    public long WarehouseId { get; set; }

    public long ZoneId { get; set; }

    public string RackCode { get; set; } = null!;

    public string RackName { get; set; } = null!;

    public decimal? AreaSquareMeter { get; set; }

    public decimal? MaxWeightKg { get; set; }

    public decimal? MaxVolumeM3 { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<StorageLocation> StorageLocations { get; set; } = new List<StorageLocation>();

    public virtual WarehouseZone WarehouseZone { get; set; } = null!;
}
