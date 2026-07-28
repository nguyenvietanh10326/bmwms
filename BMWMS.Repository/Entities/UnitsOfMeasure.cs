using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class UnitsOfMeasure
{
    public int UnitOfMeasureId { get; set; }

    public string UnitCode { get; set; } = null!;

    public string UnitName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
