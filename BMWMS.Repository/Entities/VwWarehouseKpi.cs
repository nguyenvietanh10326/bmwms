using System;
using System.Collections.Generic;

namespace BMWMS.Repository.Entities;

public partial class VwWarehouseKpi
{
    public long WarehouseId { get; set; }

    public string WarehouseCode { get; set; } = null!;

    public decimal? AverageInboundProcessingHours { get; set; }

    public decimal? AverageOutboundProcessingHours { get; set; }

    public decimal? InboundOnTimeRate { get; set; }

    public decimal? OutboundOnTimeRate { get; set; }
}
