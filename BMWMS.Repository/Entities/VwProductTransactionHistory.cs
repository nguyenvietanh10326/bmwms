using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class VwProductTransactionHistory
{
    public long InventoryTransactionId { get; set; }

    public DateTime TransactionAt { get; set; }

    public string TransactionType { get; set; } = null!;

    public long ProductId { get; set; }

    public string ProductCode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public long WarehouseId { get; set; }

    public long StorageLocationId { get; set; }

    public string LocationCode { get; set; } = null!;

    public long ProductLotId { get; set; }

    public string LotNumber { get; set; } = null!;

    public decimal OnHandDelta { get; set; }

    public decimal ReservedDelta { get; set; }

    public long? InboundOrderDetailId { get; set; }

    public long? OutboundOrderDetailId { get; set; }

    public long? TransferOrderDetailId { get; set; }

    public long? StocktakeItemId { get; set; }

    public long? InventoryReservationId { get; set; }

    public long PerformedByUserId { get; set; }

    public string PerformedBy { get; set; } = null!;

    public string? Notes { get; set; }
}
