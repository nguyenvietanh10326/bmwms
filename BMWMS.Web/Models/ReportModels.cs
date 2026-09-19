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
    
    public DateTime CutOffTime { get; set; }
}

public class InboundReportFilterModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? ProductSearch { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class InboundReportItemModel
{
    public string InboundOrderNumber { get; set; } = null!;
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string UnitCode { get; set; } = string.Empty;
    public string SourceType { get; set; } = null!;
    public DateOnly ExpectedReceiptDate { get; set; }
    public string Status { get; set; } = null!;
    public decimal ExpectedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal DamagedQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }
}

public class InboundReportResponseModel
{
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public DateTime? CutOffTime { get; set; }
    public List<InboundReportItemModel> Items { get; set; } = new();
}

// --- Outbound Report ---

public class OutboundReportFilterModel
{
    public DateTime? FromDate { get; set; } = DateTime.Today.AddDays(-30);
    public DateTime? ToDate { get; set; } = DateTime.Today;
    public string? ProductSearch { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class OutboundReportItemModel
{
    public string OutboundOrderNumber { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public DateTime ExpectedIssueDate { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal IssuedQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class OutboundReportResponseModel
{
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public DateTime? CutOffTime { get; set; }
    public List<OutboundReportItemModel> Items { get; set; } = new();
}

// --- In-Out-Stock Report ---

public class InOutStockReportFilterModel
{
    public DateTime? FromDate { get; set; } = DateTime.Today.AddDays(-30);
    public DateTime? ToDate { get; set; } = DateTime.Today;
    public string? ProductSearch { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class InOutStockReportItemModel
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal InboundQuantity { get; set; }
    public decimal OutboundQuantity { get; set; }
    public decimal AdjustmentQuantity { get; set; }
    public decimal ClosingBalance { get; set; }
}

public class InOutStockReportResponseModel
{
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public DateTime? CutOffTime { get; set; }
    public List<InOutStockReportItemModel> Items { get; set; } = new();
}

// --- Supplier Statistics Report ---

public class SupplierStatisticsFilterModel
{
    public DateTime? FromDate { get; set; } = DateTime.Today.AddDays(-30);
    public DateTime? ToDate { get; set; } = DateTime.Today;
    public string? SupplierSearch { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class SupplierStatisticsItemModel
{
    public long SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public int InboundOrderCount { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal DamagedQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }
    public decimal CompletionPerformance { get; set; }
}

public class SupplierStatisticsResponseModel
{
    public DateTime CutOffTime { get; set; }
    public PagedResultModel<SupplierStatisticsItemModel> Data { get; set; } = new();
}

public class LowStockAlertFilterModel
{
    public string? Keyword { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class LowStockAlertItemModel
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

public class LowStockAlertResponseModel
{
    public PagedResultModel<LowStockAlertItemModel> Data { get; set; } = new();
    public DateTime CutOffTime { get; set; } = DateTime.UtcNow;
}
public class ExpiringLotAlertFilterModel
{
    public string? Keyword { get; set; }
    public int? MaxDaysToExpiry { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class ExpiringLotAlertItemModel
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

public class ExpiringLotAlertResponseModel
{
    public PagedResultModel<ExpiringLotAlertItemModel> Data { get; set; } = new();
    public DateTime CutOffTime { get; set; } = DateTime.UtcNow;
}

