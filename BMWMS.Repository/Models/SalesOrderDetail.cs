using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class SalesOrderDetail
{
    public long SalesOrderDetailId { get; set; }

    public long SalesOrderId { get; set; }

    public long ProductId { get; set; }

    public decimal OrderedQuantity { get; set; }

    public decimal ReservedQuantity { get; set; }

    public decimal FulfilledQuantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<InventoryReservation> InventoryReservations { get; set; } = new List<InventoryReservation>();

    public virtual Product Product { get; set; } = null!;

    public virtual SalesOrder SalesOrder { get; set; } = null!;
}
