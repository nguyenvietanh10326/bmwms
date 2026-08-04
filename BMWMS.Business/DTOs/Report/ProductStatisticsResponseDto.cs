using System;
using System.Collections.Generic;
using BMWMS.Business.Common;

namespace BMWMS.Business.DTOs.Report;

public class ProductStatisticsResponseDto
{
    public DateTime CutOffTime { get; set; }
    public PagedResultDto<ProductStatisticsItemDto> Data { get; set; } = new();
}
