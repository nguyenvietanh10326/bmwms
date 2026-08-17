using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class SalesOrder
{
    public long SalesOrderId { get; set; }

    public string SalesOrderNumber { get; set; } = null!;

    public long CustomerId { get; set; }

    public DateOnly OrderDate { get; set; }

    public DateOnly? ExpectedIssueDate { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public string AllocationStrategy { get; set; } = "FIFO";

    public long CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? ConfirmedByUserId { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? ConfirmedByUser { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<InboundOrder> InboundOrders { get; set; } = new List<InboundOrder>();

    public virtual ICollection<OutboundOrder> OutboundOrders { get; set; } = new List<OutboundOrder>();

    public virtual ICollection<SalesOrderDetail> SalesOrderDetails { get; set; } = new List<SalesOrderDetail>();
}
