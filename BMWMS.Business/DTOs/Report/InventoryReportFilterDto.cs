namespace BMWMS.Business.DTOs.Report;

public class InventoryReportFilterDto
{
    public string? ProductSearch { get; set; }
    public string? LocationCode { get; set; }
    public string? LotNumber { get; set; }
    public bool PositiveStockOnly { get; set; } = true;
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
