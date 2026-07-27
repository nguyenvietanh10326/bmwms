using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class ProductAttribute
{
    public long ProductAttributeId { get; set; }

    public string AttributeCode { get; set; } = null!;

    public string AttributeName { get; set; } = null!;

    public string DataType { get; set; } = null!;

    public string? UnitLabel { get; set; }

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<ProductAttributeOption> ProductAttributeOptions { get; set; } = new List<ProductAttributeOption>();

    public virtual ICollection<ProductAttributeValue> ProductAttributeValues { get; set; } = new List<ProductAttributeValue>();

    public virtual ICollection<ProductGroupAttribute> ProductGroupAttributes { get; set; } = new List<ProductGroupAttribute>();
}
