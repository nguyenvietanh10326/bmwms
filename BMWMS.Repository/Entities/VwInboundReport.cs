using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class VwInboundReport
{
    public long InboundOrderId { get; set; }

    public string InboundOrderNumber { get; set; } = null!;

    public string SourceType { get; set; } = null!;

    public long? PurchaseOrderId { get; set; }

    public long? SalesOrderId { get; set; }

    public long? TransferOrderId { get; set; }

    public long? ParentInboundOrderId { get; set; }

    public long WarehouseId { get; set; }

    public DateOnly ExpectedReceiptDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? ConfirmedAt { get; set; }

    public long ProductId { get; set; }

    public string ProductCode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public decimal ExpectedQuantity { get; set; }

    public decimal ReceivedQuantity { get; set; }

    public decimal DamagedQuantity { get; set; }

    public decimal ShortageQuantity { get; set; }
}
