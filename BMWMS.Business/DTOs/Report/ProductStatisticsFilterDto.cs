using System;

namespace BMWMS.Business.DTOs.Report;

public class ProductStatisticsFilterDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? ProductSearch { get; set; }
    public string? ProductGroupCode { get; set; }
    
    // pagination
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
