using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class Inventory
{
    public long InventoryId { get; set; }

    public long ProductId { get; set; }

    public long StorageLocationId { get; set; }

    public long ProductLotId { get; set; }

    public decimal OnHandQuantity { get; set; }

    public decimal ReservedQuantity { get; set; }

    public decimal? AvailableQuantity { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ProductLot ProductLot { get; set; } = null!;

    public virtual StorageLocation StorageLocation { get; set; } = null!;
}
