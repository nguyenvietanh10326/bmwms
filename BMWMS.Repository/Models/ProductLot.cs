using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class ProductLot
{
    public long ProductLotId { get; set; }

    public long ProductId { get; set; }

    public string LotNumber { get; set; } = null!;

    public DateOnly? ManufactureDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public DateOnly FirstReceivedDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<InboundOrderDetail> InboundOrderDetails { get; set; } = new List<InboundOrderDetail>();

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<InventoryReservation> InventoryReservations { get; set; } = new List<InventoryReservation>();

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<OutboundOrderDetail> OutboundOrderDetails { get; set; } = new List<OutboundOrderDetail>();

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<StocktakeItem> StocktakeItems { get; set; } = new List<StocktakeItem>();

    public virtual ICollection<TransferOrderDetail> TransferOrderDetails { get; set; } = new List<TransferOrderDetail>();
}
