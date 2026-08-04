using System;

namespace BMWMS.Business.DTOs.Report;

public class SupplierStatisticsFilterDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? SupplierSearch { get; set; }
    
    // pagination
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
