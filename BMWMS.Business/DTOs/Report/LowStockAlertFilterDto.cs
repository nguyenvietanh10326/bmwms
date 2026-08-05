namespace BMWMS.Business.DTOs.Report;

public class LowStockAlertFilterDto
{
    public string? Keyword { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
