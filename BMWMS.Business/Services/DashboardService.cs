using BMWMS.Business.DTOs.Dashboard;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Business.Services;

public class DashboardService : IDashboardService
{
    private readonly BmwmsContext _context;

    public DashboardService(BmwmsContext context)
    {
        _context = context;
    }

    public async Task<DashboardResponseDto> GetDashboardDataAsync(long? warehouseId = null)
    {
        var response = new DashboardResponseDto();
        var currentMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        // 1. Total Products
        response.TotalProducts = await _context.Products.CountAsync();
        response.NewProductsThisMonth = await _context.Products.CountAsync(p => p.CreatedAt >= currentMonthStart);

        // 2. Active Warehouses
        response.ActiveWarehouses = await _context.Warehouses.CountAsync(w => w.Status == "ACTIVE");

        // 3. Inventory Alerts (Low Stock / Out of Stock)
        var lowStockAlerts = await _context.Set<VwLowStockAlert>().ToListAsync();
        
        response.LowStockCount = lowStockAlerts.Count(a => a.AvailableQuantity > 0 && a.AvailableQuantity <= a.MinimumStockQuantity);
        response.OutOfStockCount = lowStockAlerts.Count(a => a.AvailableQuantity <= 0);

        foreach (var alert in lowStockAlerts.Take(5)) // Limit to top 5 for UI
        {
            response.InventoryAlerts.Add(new DashboardAlertDto
            {
                ProductName = alert.ProductName,
                AvailableQuantity = alert.AvailableQuantity ?? 0m,
                Threshold = alert.MinimumStockQuantity,
                Status = (alert.AvailableQuantity <= 0) ? "Hết hàng" : "Sắp hết",
                Unit = "" // Need to resolve if view doesn't have it
            });
        }

        // 4. Order statuses
        response.PendingPurchaseOrders = await _context.Set<PurchaseOrder>().CountAsync(po =>
            po.Status == "DRAFT" ||
            po.Status == "PENDING_CONFIRMATION" ||
            po.Status == "PENDING_REMAINDER_CONFIRMATION" ||
            po.Status == "CONFIRMED" ||
            po.Status == "PENDING_RECEIPT_REVIEW" ||
            po.Status == "PARTIALLY_RECEIVED");
        response.PendingSalesOrders = await _context.Set<SalesOrder>().CountAsync(so => so.Status == "WAITING_STOCK");
        response.ProcessingInboundOrders = await _context.InboundOrders.CountAsync(io => io.Status == "IN_PROGRESS" || io.Status == "ASSIGNED");
        response.PickingOutboundOrders = await _context.OutboundOrders.CountAsync(oo => oo.Status == "PICKING");

        // 5. Recent Activities
        var activities = await _context.Set<VwProductTransactionHistory>()
            .OrderByDescending(t => t.TransactionAt)
            .Take(5)
            .ToListAsync();

        string[] colors = { "purple", "blue", "green", "orange", "red" };
        int colorIdx = 0;

        foreach (var act in activities)
        {
            string actionText = $"{act.TransactionType} {act.ProductName} - Số lượng: {act.OnHandDelta}";
            if (!string.IsNullOrEmpty(act.Notes))
            {
                actionText = act.Notes;
            }

            response.RecentActivities.Add(new DashboardActivityDto
            {
                ActivityText = actionText,
                PerformedBy = act.PerformedBy ?? "Hệ thống",
                CreatedAt = act.TransactionAt,
                ColorType = colors[colorIdx % colors.Length]
            });
            colorIdx++;
        }

        return response;
    }
}
