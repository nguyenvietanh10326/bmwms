namespace BMWMS.Web.Models;

public class InventoryReportFilterModel
{
    public string? ProductSearch { get; set; }
    public string? LocationCode { get; set; }
    public string? LotNumber { get; set; }
    public bool PositiveStockOnly { get; set; } = true;
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class InventoryReportItemModel
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

public class InventoryReportResponseModel
{
    public PagedResultModel<InventoryReportItemModel> Data { get; set; } = new();
    
    public decimal TotalOnHand { get; set; }
    public decimal TotalReserved { get; set; }
    public decimal TotalAvailable { get; set; }
    
    public DateTime CutOffTime { get; set; }
}
