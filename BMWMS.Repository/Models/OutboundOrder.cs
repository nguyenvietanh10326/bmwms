using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class OutboundOrder
{
    public long OutboundOrderId { get; set; }

    public string OutboundOrderNumber { get; set; } = null!;

    public long WarehouseId { get; set; }

    public string SourceType { get; set; } = null!;

    public long? SalesOrderId { get; set; }

    public long? PurchaseOrderId { get; set; }

    public long? TransferOrderId { get; set; }

    public DateOnly ExpectedIssueDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public long CreatedByUserId { get; set; }

    public long? AssignedToUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? ConfirmedByUserId { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public long? CancelledByUserId { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? CancellationReason { get; set; }

    public virtual User? AssignedToUser { get; set; }

    public virtual User? CancelledByUser { get; set; }

    public virtual User? ConfirmedByUser { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<OutboundOrderItem> OutboundOrderItems { get; set; } = new List<OutboundOrderItem>();

    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    public virtual SalesOrder? SalesOrder { get; set; }

    public virtual TransferOrder? TransferOrder { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;
}
