using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class ProductGroupAttribute
{
    public long ProductGroupId { get; set; }

    public long ProductAttributeId { get; set; }

    public bool IsRequired { get; set; }

    public int DisplayOrder { get; set; }

    public string? DefaultValue { get; set; }

    public virtual ProductAttribute ProductAttribute { get; set; } = null!;

    public virtual ProductGroup ProductGroup { get; set; } = null!;
}
