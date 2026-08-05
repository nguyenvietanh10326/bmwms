namespace BMWMS.Business.DTOs.Report;

public class LowStockAlertItemDto
{
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal MinimumStockQuantity { get; set; }
    public decimal? AvailableQuantity { get; set; }
    public decimal? ShortageQuantity { get; set; }
}
