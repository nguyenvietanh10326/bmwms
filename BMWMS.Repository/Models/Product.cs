using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class Product
{
    public long ProductId { get; set; }

    public long ProductGroupId { get; set; }

    public int UnitOfMeasureId { get; set; }

    public string ProductCode { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public string? Barcode { get; set; }

    public string? Description { get; set; }

    public string RotationMethod { get; set; } = null!;

    public bool TrackLot { get; set; }

    public bool TrackExpiry { get; set; }

    public int? DefaultShelfLifeDays { get; set; }

    public string Status { get; set; } = null!;

    public long CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? UpdatedByUserId { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<InboundOrderItem> InboundOrderItems { get; set; } = new List<InboundOrderItem>();

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();

    public virtual ICollection<OutboundOrderItem> OutboundOrderItems { get; set; } = new List<OutboundOrderItem>();

    public virtual ICollection<ProductAttributeValue> ProductAttributeValues { get; set; } = new List<ProductAttributeValue>();

    public virtual ProductFixedLocation? ProductFixedLocation { get; set; }

    public virtual ProductGroup ProductGroup { get; set; } = null!;

    public virtual ICollection<ProductLot> ProductLots { get; set; } = new List<ProductLot>();

    public virtual ICollection<ProductWarehousePolicy> ProductWarehousePolicies { get; set; } = new List<ProductWarehousePolicy>();

    public virtual ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();

    public virtual ICollection<SalesOrderDetail> SalesOrderDetails { get; set; } = new List<SalesOrderDetail>();

    public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();

    public virtual ICollection<TransferOrderDetail> TransferOrderDetails { get; set; } = new List<TransferOrderDetail>();

    public virtual UnitsOfMeasure UnitOfMeasure { get; set; } = null!;

    public virtual User? UpdatedByUser { get; set; }
}
