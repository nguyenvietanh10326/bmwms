using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class VwStocktakeResult
{
    public long StocktakeSessionId { get; set; }

    public string StocktakeNumber { get; set; } = null!;

    public long WarehouseId { get; set; }

    public DateOnly PlannedDate { get; set; }

    public string Status { get; set; } = null!;

    public long StorageLocationId { get; set; }

    public string LocationCode { get; set; } = null!;

    public string CountStatus { get; set; } = null!;

    public long? ProductId { get; set; }

    public string? ProductCode { get; set; }

    public string? ProductName { get; set; }

    public long? ProductLotId { get; set; }

    public string? LotNumber { get; set; }

    public decimal? BookQuantity { get; set; }

    public decimal? CountedQuantity { get; set; }

    public decimal? DifferenceQuantity { get; set; }

    public decimal? AdjustmentQuantity { get; set; }

    public string? Resolution { get; set; }
}
