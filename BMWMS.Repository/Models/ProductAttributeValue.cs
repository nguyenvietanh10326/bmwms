using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class ProductAttributeValue
{
    public long ProductId { get; set; }

    public long ProductAttributeId { get; set; }

    public string AttributeValue { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ProductAttribute ProductAttribute { get; set; } = null!;
}
