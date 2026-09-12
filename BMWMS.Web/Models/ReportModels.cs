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
    public List<InOutStockReportItemModel> Items { get; set; } = new();
}

// --- Product Statistics Report ---

public class ProductStatisticsFilterModel
{
    public DateTime? FromDate { get; set; } = DateTime.Today.AddDays(-30);
    public DateTime? ToDate { get; set; } = DateTime.Today;
    public string? ProductSearch { get; set; }
    public string? ProductGroupCode { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class ProductStatisticsItemModel
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string BaseUnitCode { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal InboundQuantity { get; set; }
    public decimal OutboundQuantity { get; set; }
    public decimal AdjustmentQuantity { get; set; }
    public int MovementFrequency { get; set; }
    public int DaysSinceLastMovement { get; set; }
}

public class ProductStatisticsResponseModel
{
    public DateTime CutOffTime { get; set; }
    public PagedResultModel<ProductStatisticsItemModel> Data { get; set; } = new();
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

// --- Stocktake Statistics Report ---

public class StocktakeStatisticsFilterModel
{
    public DateTime? FromDate { get; set; } = DateTime.Today.AddDays(-30);
    public DateTime? ToDate { get; set; } = DateTime.Today;
    public string? CountType { get; set; }
    public string? StorageAreaCode { get; set; }
    public string? ProductGroupCode { get; set; }
    public string? SessionStatus { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class StocktakeStatisticsItemModel
{
    public long StocktakeSessionId { get; set; }
    public string StocktakeNumber { get; set; } = string.Empty;
    public DateOnly PlannedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int BinsCounted { get; set; }
    public int MatchedItems { get; set; }
    public int ShortageItems { get; set; }
    public int ExcessItems { get; set; }
    public int TotalItemsCounted { get; set; }
    public decimal TotalShortageQuantity { get; set; }
    public decimal TotalExcessQuantity { get; set; }
    public decimal TotalApprovedAdjustmentQuantity { get; set; }
    public double StockAccuracyRate => TotalItemsCounted > 0 ? (double)MatchedItems / TotalItemsCounted * 100.0 : 0;
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

public class OverdueOrderAlertFilterModel
{
    public string? DocumentType { get; set; }
    public string? Keyword { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class OverdueOrderAlertItemModel
{
    public string? DocumentType { get; set; }
    public long DocumentId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public DateOnly? DueDate { get; set; }
    public int? DaysOverdue { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class OverdueOrderAlertResponseModel
{
    public PagedResultModel<OverdueOrderAlertItemModel> Data { get; set; } = new();
    public DateTime CutOffTime { get; set; } = DateTime.UtcNow;
}

public class StocktakeStatisticsResponseModel
{
    public DateTime CutOffTime { get; set; }
    public PagedResultModel<StocktakeStatisticsItemModel> Data { get; set; } = new();
}

public class WarehouseKpiItemModel
{
    public long WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public decimal? AverageInboundProcessingHours { get; set; }
    public decimal? AverageOutboundProcessingHours { get; set; }
    public decimal? InboundOnTimeRate { get; set; }
    public decimal? OutboundOnTimeRate { get; set; }
}

public class WarehouseKpiResponseModel
{
    public List<WarehouseKpiItemModel> Items { get; set; } = new();
}
