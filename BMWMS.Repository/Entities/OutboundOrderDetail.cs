using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class OutboundOrderDetail
{
    public long OutboundOrderDetailId { get; set; }

    public long OutboundOrderItemId { get; set; }

    public long OutboundOrderId { get; set; }

    public long ProductId { get; set; }

    public long StorageLocationId { get; set; }

    public long ProductLotId { get; set; }

    public long? InventoryReservationId { get; set; }

    public decimal IssuedQuantity { get; set; }

    public long RecordedByUserId { get; set; }

    public DateTime RecordedAt { get; set; }

    public string? Notes { get; set; }

    public virtual InventoryReservation? InventoryReservation { get; set; }

    public virtual InventoryTransaction? InventoryTransaction { get; set; }

    public virtual OutboundOrderItem OutboundOrderItem { get; set; } = null!;

    public virtual ProductLot ProductLot { get; set; } = null!;

    public virtual User RecordedByUser { get; set; } = null!;

    public virtual StorageLocation StorageLocation { get; set; } = null!;
}
