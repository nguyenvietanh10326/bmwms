using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class TransferOrderDetail
{
    public long TransferOrderDetailId { get; set; }

    public long TransferOrderId { get; set; }

    public long ProductId { get; set; }

    public long? ProductLotId { get; set; }

    public long? SourceLocationId { get; set; }

    public long? DestinationLocationId { get; set; }

    public decimal RequestedQuantity { get; set; }

    public decimal MovedQuantity { get; set; }

    public string? StaffNote { get; set; }

    public long? ConfirmedByUserId { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public virtual User? ConfirmedByUser { get; set; }

    public virtual StorageLocation? DestinationLocation { get; set; }

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual Product Product { get; set; } = null!;

    public virtual ProductLot? ProductLot { get; set; }

    public virtual StorageLocation? SourceLocation { get; set; }

    public virtual TransferOrder TransferOrder { get; set; } = null!;
}
