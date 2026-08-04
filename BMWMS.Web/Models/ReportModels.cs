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
    public decimal TotalExpected { get; set; }
    public decimal TotalReceived { get; set; }
    public decimal TotalDamaged { get; set; }
    public decimal TotalShortage { get; set; }
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
    public string SourceType { get; set; } = string.Empty;
    public DateTime ExpectedIssueDate { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal IssuedQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class OutboundReportResponseModel
{
    public decimal TotalRequested { get; set; }
    public decimal TotalIssued { get; set; }
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
