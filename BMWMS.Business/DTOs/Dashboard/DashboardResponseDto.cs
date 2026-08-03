namespace BMWMS.Business.DTOs.Dashboard;

public class DashboardResponseDto
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

    public List<DashboardAlertDto> InventoryAlerts { get; set; } = new();
    public List<DashboardActivityDto> RecentActivities { get; set; } = new();
}
