using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class PurchaseOrderDetail
{
    public long PurchaseOrderDetailId { get; set; }

    public long PurchaseOrderId { get; set; }

    public long ProductId { get; set; }

    public decimal OrderedQuantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public string? Notes { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;
}
