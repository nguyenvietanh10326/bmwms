using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class Supplier
{
    public long SupplierId { get; set; }

    public string SupplierCode { get; set; } = null!;

    public string SupplierName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? Email { get; set; }

    public string Address { get; set; } = null!;

    public string RepresentativeName { get; set; } = null!;

    public string TaxCode { get; set; } = null!;

    public string Status { get; set; } = null!;

    public long CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? UpdatedByUserId { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User CreatedByUser { get; set; } = null!;

    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();

    public virtual ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();

    public virtual User? UpdatedByUser { get; set; }
}
