using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class VwOutboundReport
{
    public long OutboundOrderId { get; set; }

    public string OutboundOrderNumber { get; set; } = null!;

    public string SourceType { get; set; } = null!;

    public long? SalesOrderId { get; set; }

    public long? PurchaseOrderId { get; set; }

    public long? TransferOrderId { get; set; }

    public long WarehouseId { get; set; }

    public DateOnly ExpectedIssueDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? ConfirmedAt { get; set; }

    public long ProductId { get; set; }

    public string ProductCode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public decimal RequestedQuantity { get; set; }

    public decimal IssuedQuantity { get; set; }
}
