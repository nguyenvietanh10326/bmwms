using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Models;

public partial class VwOverdueOrder
{
    public string? DocumentType { get; set; }

    public long DocumentId { get; set; }

    public string DocumentNumber { get; set; } = null!;

    public long WarehouseId { get; set; }

    public DateOnly? DueDate { get; set; }

    public int? DaysOverdue { get; set; }

    public string Status { get; set; } = null!;
}
