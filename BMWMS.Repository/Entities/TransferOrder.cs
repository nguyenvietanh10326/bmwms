using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class TransferOrder
{
    public long TransferOrderId { get; set; }

    public string TransferOrderNumber { get; set; } = null!;

    public string TransferType { get; set; } = null!;

    public long SourceWarehouseId { get; set; }

    public long DestinationWarehouseId { get; set; }

    public DateOnly RequestedDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public long CreatedByUserId { get; set; }

    public long? AssignedToUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? ConfirmedByUserId { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public virtual User? AssignedToUser { get; set; }

    public virtual User? ConfirmedByUser { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual Warehouse DestinationWarehouse { get; set; } = null!;

    public virtual ICollection<InboundOrder> InboundOrders { get; set; } = new List<InboundOrder>();

    public virtual ICollection<OutboundOrder> OutboundOrders { get; set; } = new List<OutboundOrder>();

    public virtual Warehouse SourceWarehouse { get; set; } = null!;

    public virtual ICollection<TransferOrderDetail> TransferOrderDetails { get; set; } = new List<TransferOrderDetail>();
}
