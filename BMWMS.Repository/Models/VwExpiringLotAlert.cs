using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class VwExpiringLotAlert
{
    public long WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = null!;

    public long ProductId { get; set; }

    public string ProductCode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public long ProductLotId { get; set; }

    public string LotNumber { get; set; } = null!;

    public DateOnly? ExpiryDate { get; set; }

    public int? DaysToExpiry { get; set; }

    public decimal? OnHandQuantity { get; set; }

    public decimal? AvailableQuantity { get; set; }

    public int ExpiryWarningDays { get; set; }
}
