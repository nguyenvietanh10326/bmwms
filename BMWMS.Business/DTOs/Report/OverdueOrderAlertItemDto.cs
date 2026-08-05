namespace BMWMS.Business.DTOs.Report;

public class OverdueOrderAlertItemDto
{
    public string? DocumentType { get; set; }
    public long DocumentId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public DateOnly? DueDate { get; set; }
    public int? DaysOverdue { get; set; }
    public string Status { get; set; } = string.Empty;
}
