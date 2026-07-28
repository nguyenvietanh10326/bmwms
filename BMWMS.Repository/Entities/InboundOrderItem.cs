using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class InboundOrderItem
{
    public long InboundOrderItemId { get; set; }

    public long InboundOrderId { get; set; }

    public long ProductId { get; set; }

    public decimal ExpectedQuantity { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public decimal DamagedQuantity { get; set; }

    public decimal ShortageQuantity { get; set; }

    public string? Notes { get; set; }

    public virtual InboundOrder InboundOrder { get; set; } = null!;

    public virtual ICollection<InboundOrderDetail> InboundOrderDetails { get; set; } = new List<InboundOrderDetail>();

    public virtual Product Product { get; set; } = null!;
}
