using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class InventoryTransaction
{
    public long InventoryTransactionId { get; set; }

    public string TransactionType { get; set; } = null!;

    public long ProductId { get; set; }

    public long StorageLocationId { get; set; }

    public long ProductLotId { get; set; }

    public decimal OnHandDelta { get; set; }

    public decimal ReservedDelta { get; set; }

    public long? InboundOrderDetailId { get; set; }

    public long? OutboundOrderDetailId { get; set; }

    public long? TransferOrderDetailId { get; set; }

    public long? StocktakeItemId { get; set; }

    public long? InventoryReservationId { get; set; }

    public long PerformedByUserId { get; set; }

    public DateTime TransactionAt { get; set; }

    public string? Notes { get; set; }

    public virtual InboundOrderDetail? InboundOrderDetail { get; set; }

    public virtual InventoryReservation? InventoryReservation { get; set; }

    public virtual OutboundOrderDetail? OutboundOrderDetail { get; set; }

    public virtual User PerformedByUser { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual ProductLot ProductLot { get; set; } = null!;

    public virtual StocktakeItem? StocktakeItem { get; set; }

    public virtual StorageLocation StorageLocation { get; set; } = null!;

    public virtual TransferOrderDetail? TransferOrderDetail { get; set; }
}
