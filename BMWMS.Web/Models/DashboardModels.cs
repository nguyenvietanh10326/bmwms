namespace BMWMS.Web.Models;

public class DashboardResponseModel
{
    public int TotalProducts { get; set; }
    public int NewProductsThisMonth { get; set; }
    public int ActiveWarehouses { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public int PendingPurchaseOrders { get; set; }
    public int PendingSalesOrders { get; set; }
    public int ProcessingInboundOrders { get; set; }
    public int PickingOutboundOrders { get; set; }

    public List<DashboardAlertModel> InventoryAlerts { get; set; } = new();
    public List<DashboardActivityModel> RecentActivities { get; set; } = new();
}

public class DashboardAlertModel
{
    public string ProductName { get; set; } = string.Empty;
    public decimal AvailableQuantity { get; set; }
    public decimal Threshold { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}

public class DashboardActivityModel
{
    public string ActivityText { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string ColorType { get; set; } = "blue";
}
