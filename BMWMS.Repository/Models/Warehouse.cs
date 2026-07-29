using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class Warehouse
{
    public long WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = null!;

    public string WarehouseName { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public bool IsPrimary { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<InboundOrder> InboundOrders { get; set; } = new List<InboundOrder>();

    public virtual ICollection<OutboundOrder> OutboundOrders { get; set; } = new List<OutboundOrder>();

    public virtual ICollection<ProductWarehousePolicy> ProductWarehousePolicies { get; set; } = new List<ProductWarehousePolicy>();

    public virtual ICollection<StocktakeSchedule> StocktakeSchedules { get; set; } = new List<StocktakeSchedule>();

    public virtual ICollection<StocktakeSession> StocktakeSessions { get; set; } = new List<StocktakeSession>();

    public virtual ICollection<StorageLocation> StorageLocations { get; set; } = new List<StorageLocation>();

    public virtual ICollection<TransferOrder> TransferOrderDestinationWarehouses { get; set; } = new List<TransferOrder>();

    public virtual ICollection<TransferOrder> TransferOrderSourceWarehouses { get; set; } = new List<TransferOrder>();

    public virtual ICollection<WarehouseZone> WarehouseZones { get; set; } = new List<WarehouseZone>();
}
