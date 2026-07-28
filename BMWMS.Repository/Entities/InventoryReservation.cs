using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class InventoryReservation
{
    public long InventoryReservationId { get; set; }

    public long SalesOrderDetailId { get; set; }

    public long ProductId { get; set; }

    public long StorageLocationId { get; set; }

    public long ProductLotId { get; set; }

    public decimal ReservedQuantity { get; set; }

    public decimal ConsumedQuantity { get; set; }

    public string Status { get; set; } = null!;

    public long ReservedByUserId { get; set; }

    public DateTime ReservedAt { get; set; }

    public DateTime? ReleasedAt { get; set; }

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<OutboundOrderDetail> OutboundOrderDetails { get; set; } = new List<OutboundOrderDetail>();

    public virtual ProductLot ProductLot { get; set; } = null!;

    public virtual User ReservedByUser { get; set; } = null!;

    public virtual SalesOrderDetail SalesOrderDetail { get; set; } = null!;

    public virtual StorageLocation StorageLocation { get; set; } = null!;
}
