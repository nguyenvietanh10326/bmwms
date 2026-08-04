using System;
using System.Collections.Generic;
using BMWMS.Business.Common;

namespace BMWMS.Business.DTOs.Report;

public class SupplierStatisticsResponseDto
{
    public DateTime CutOffTime { get; set; }
    public PagedResultDto<SupplierStatisticsItemDto> Data { get; set; } = new();
}
