namespace BMWMS.Business.DTOs.Report;

public class ExpiringLotAlertFilterDto
{
    public string? Keyword { get; set; }
    public int? MaxDaysToExpiry { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
