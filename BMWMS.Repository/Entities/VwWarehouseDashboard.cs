using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class VwWarehouseDashboard
{
    public long WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = null!;

    public string WarehouseName { get; set; } = null!;

    public decimal? TotalOnHandQuantity { get; set; }

    public decimal? TotalAvailableQuantity { get; set; }

    public int? ProductCountInStock { get; set; }

    public int? BinCount { get; set; }

    public int? OccupiedBinCount { get; set; }

    public int? PendingInboundCount { get; set; }

    public int? PendingOutboundCount { get; set; }

    public int? LowStockAlertCount { get; set; }

    public int? ExpiringLotAlertCount { get; set; }
}
