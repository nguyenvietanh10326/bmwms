using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class ProductGroup
{
    public long ProductGroupId { get; set; }

    public long? ParentGroupId { get; set; }

    public string GroupCode { get; set; } = null!;

    public string GroupName { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ProductGroup> InverseParentGroup { get; set; } = new List<ProductGroup>();

    public virtual ProductGroup? ParentGroup { get; set; }

    public virtual ICollection<ProductGroupAttribute> ProductGroupAttributes { get; set; } = new List<ProductGroupAttribute>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
