using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class VwInventoryInOutSummary
{
    public DateOnly? TransactionDate { get; set; }

    public long WarehouseId { get; set; }

    public long ProductId { get; set; }

    public string ProductCode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public decimal? InboundQuantity { get; set; }

    public decimal? OutboundQuantity { get; set; }

    public decimal? AdjustmentQuantity { get; set; }

    public decimal? NetMovementQuantity { get; set; }
}
