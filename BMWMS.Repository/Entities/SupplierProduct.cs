using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class SupplierProduct
{
    public long SupplierId { get; set; }

    public long ProductId { get; set; }

    public string? SupplierProductCode { get; set; }

    public decimal? LastPurchasePrice { get; set; }

    public int? LeadTimeDays { get; set; }

    public bool IsPreferred { get; set; }

    public string Status { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual Supplier Supplier { get; set; } = null!;
}
