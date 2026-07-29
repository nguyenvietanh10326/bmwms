using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class InboundOrderDetail
{
    public long InboundOrderDetailId { get; set; }

    public long InboundOrderItemId { get; set; }

    public long InboundOrderId { get; set; }

    public long ProductId { get; set; }

    public long StorageLocationId { get; set; }

    public long ProductLotId { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public string ConditionStatus { get; set; } = null!;

    public long RecordedByUserId { get; set; }

    public DateTime RecordedAt { get; set; }

    public string? Notes { get; set; }

    public virtual InboundOrderItem InboundOrderItem { get; set; } = null!;

    public virtual InventoryTransaction? InventoryTransaction { get; set; }

    public virtual ProductLot ProductLot { get; set; } = null!;

    public virtual User RecordedByUser { get; set; } = null!;

    public virtual StorageLocation StorageLocation { get; set; } = null!;
}
