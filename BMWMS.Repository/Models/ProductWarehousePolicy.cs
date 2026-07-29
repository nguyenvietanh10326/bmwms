using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class ProductWarehousePolicy
{
    public long ProductId { get; set; }

    public long WarehouseId { get; set; }

    public decimal MinimumStockQuantity { get; set; }

    public int ExpiryWarningDays { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
