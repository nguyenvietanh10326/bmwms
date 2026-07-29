using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class StocktakeItem
{
    public long StocktakeItemId { get; set; }

    public long StocktakeSessionId { get; set; }

    public long StorageLocationId { get; set; }

    public long ProductId { get; set; }

    public long ProductLotId { get; set; }

    public decimal BookQuantity { get; set; }

    public decimal? CountedQuantity { get; set; }

    public decimal? DifferenceQuantity { get; set; }

    public decimal? AdjustmentQuantity { get; set; }

    public string? Resolution { get; set; }

    public long? CountedByUserId { get; set; }

    public DateTime? CountedAt { get; set; }

    public long? ApprovedByUserId { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? Notes { get; set; }

    public virtual User? ApprovedByUser { get; set; }

    public virtual User? CountedByUser { get; set; }

    public virtual InventoryTransaction? InventoryTransaction { get; set; }

    public virtual ProductLot ProductLot { get; set; } = null!;

    public virtual StocktakeLocation StocktakeLocation { get; set; } = null!;

    public virtual StocktakeSession StocktakeSession { get; set; } = null!;

    public virtual StorageLocation StorageLocation { get; set; } = null!;
}
