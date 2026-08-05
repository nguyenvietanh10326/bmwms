namespace BMWMS.Business.DTOs.Report;

public class OverdueOrderAlertFilterDto
{
    public string? DocumentType { get; set; }
    public string? Keyword { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
