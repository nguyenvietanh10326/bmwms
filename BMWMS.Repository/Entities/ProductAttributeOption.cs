using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class ProductAttributeOption
{
    public long ProductAttributeOptionId { get; set; }

    public long ProductAttributeId { get; set; }

    public string OptionCode { get; set; } = null!;

    public string OptionValue { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual ProductAttribute ProductAttribute { get; set; } = null!;
}
