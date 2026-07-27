using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class VwLowStockAlert
{
    public long WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = null!;

    public long ProductId { get; set; }

    public string ProductCode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public decimal MinimumStockQuantity { get; set; }

    public decimal? AvailableQuantity { get; set; }

    public decimal? ShortageQuantity { get; set; }
}
