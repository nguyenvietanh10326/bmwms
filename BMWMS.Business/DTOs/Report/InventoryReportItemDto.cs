namespace BMWMS.Business.DTOs.Report;

public class InventoryReportItemDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public decimal OnHandQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
}
