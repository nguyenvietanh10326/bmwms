using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class VwInventoryAvailability
{
    public long WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = null!;

    public string WarehouseName { get; set; } = null!;

    public long StorageLocationId { get; set; }

    public string LocationCode { get; set; } = null!;

    public string LocationType { get; set; } = null!;

    public long ProductId { get; set; }

    public string ProductCode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public string UnitCode { get; set; } = null!;

    public long ProductLotId { get; set; }

    public string LotNumber { get; set; } = null!;

    public DateOnly FirstReceivedDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public decimal OnHandQuantity { get; set; }

    public decimal ReservedQuantity { get; set; }

    public decimal? AvailableQuantity { get; set; }

    public string RotationMethod { get; set; } = null!;

    public DateTime LastUpdatedAt { get; set; }
}
