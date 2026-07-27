using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class ProductFixedLocation
{
    public long ProductId { get; set; }

    public long StorageLocationId { get; set; }

    public int Priority { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual StorageLocation StorageLocation { get; set; } = null!;
}
