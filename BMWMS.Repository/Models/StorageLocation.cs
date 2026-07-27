using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class StorageLocation
{
    public long StorageLocationId { get; set; }

    public long WarehouseId { get; set; }

    public long? RackId { get; set; }

    public string LocationCode { get; set; } = null!;

    public string? LocationName { get; set; }

    public string LocationType { get; set; } = null!;

    public decimal? AreaSquareMeter { get; set; }

    public decimal? MaxWeightKg { get; set; }

    public decimal? MaxVolumeM3 { get; set; }

    public bool IsPutawayAllowed { get; set; }

    public bool IsPickable { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<InboundOrderDetail> InboundOrderDetails { get; set; } = new List<InboundOrderDetail>();

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<InventoryReservation> InventoryReservations { get; set; } = new List<InventoryReservation>();

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<OutboundOrderDetail> OutboundOrderDetails { get; set; } = new List<OutboundOrderDetail>();

    public virtual ICollection<ProductFixedLocation> ProductFixedLocations { get; set; } = new List<ProductFixedLocation>();

    public virtual ICollection<StocktakeItem> StocktakeItems { get; set; } = new List<StocktakeItem>();

    public virtual ICollection<StocktakeLocation> StocktakeLocations { get; set; } = new List<StocktakeLocation>();

    public virtual StorageRack? StorageRack { get; set; }

    public virtual ICollection<TransferOrderDetail> TransferOrderDetailDestinationLocations { get; set; } = new List<TransferOrderDetail>();

    public virtual ICollection<TransferOrderDetail> TransferOrderDetailSourceLocations { get; set; } = new List<TransferOrderDetail>();

    public virtual Warehouse Warehouse { get; set; } = null!;
}
