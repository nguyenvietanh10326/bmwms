using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class StocktakeLocation
{
    public long StocktakeSessionId { get; set; }

    public long StorageLocationId { get; set; }

    public string CountStatus { get; set; } = null!;

    public long? CountedByUserId { get; set; }

    public DateTime? CountedAt { get; set; }

    public string? Notes { get; set; }

    public virtual User? CountedByUser { get; set; }

    public virtual ICollection<StocktakeItem> StocktakeItems { get; set; } = new List<StocktakeItem>();

    public virtual StocktakeSession StocktakeSession { get; set; } = null!;

    public virtual StorageLocation StorageLocation { get; set; } = null!;
}
