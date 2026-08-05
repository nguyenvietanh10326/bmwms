namespace BMWMS.Business.DTOs.Report;

public class ExpiringLotAlertItemDto
{
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public long ProductLotId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public int? DaysToExpiry { get; set; }
    public decimal? OnHandQuantity { get; set; }
    public decimal? AvailableQuantity { get; set; }
    public int ExpiryWarningDays { get; set; }
}
