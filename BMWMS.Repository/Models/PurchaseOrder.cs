using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class PurchaseOrder
{
    public long PurchaseOrderId { get; set; }

    public string PurchaseOrderNumber { get; set; } = null!;

    public long SupplierId { get; set; }

    public DateOnly OrderDate { get; set; }

    public DateOnly? ExpectedDeliveryDate { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public long CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? ConfirmedByUserId { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? ConfirmedByUser { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<InboundOrder> InboundOrders { get; set; } = new List<InboundOrder>();

    public virtual ICollection<OutboundOrder> OutboundOrders { get; set; } = new List<OutboundOrder>();

    public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();

    public virtual Supplier Supplier { get; set; } = null!;
}
