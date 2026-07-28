using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class OutboundOrderItem
{
    public long OutboundOrderItemId { get; set; }

    public long OutboundOrderId { get; set; }

    public long ProductId { get; set; }

    public decimal RequestedQuantity { get; set; }

    public decimal IssuedQuantity { get; set; }

    public string? Notes { get; set; }

    public virtual OutboundOrder OutboundOrder { get; set; } = null!;

    public virtual ICollection<OutboundOrderDetail> OutboundOrderDetails { get; set; } = new List<OutboundOrderDetail>();

    public virtual Product Product { get; set; } = null!;
}
