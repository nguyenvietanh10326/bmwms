namespace BMWMS.Business.DTOs.Report;

public class StocktakeStatisticsFilterDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    
    public string? CountType { get; set; }
    public string? StorageAreaCode { get; set; }
    public string? ProductGroupCode { get; set; }
    public string? SessionStatus { get; set; }
    
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
