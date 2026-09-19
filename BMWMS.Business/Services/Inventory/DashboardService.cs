using BMWMS.Business.DTOs.Inventory;
using BMWMS.Business.Interfaces.Inventory;
using BMWMS.Repository.Interfaces.Inventory;

namespace BMWMS.Business.Services.Inventory
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
        {
            var totalProducts = await _dashboardRepository.GetTotalProductsCountAsync();
            var activeWarehouses = await _dashboardRepository.GetActiveWarehousesCountAsync();

            var pendingPO = await _dashboardRepository.GetPendingPurchaseOrdersCountAsync();
            var pendingSO = await _dashboardRepository.GetPendingSalesOrdersCountAsync();
            var processingInbound = await _dashboardRepository.GetProcessingInboundOrdersCountAsync();
            var pickingOutbound = await _dashboardRepository.GetPickingOutboundOrdersCountAsync();

            // Lấy cảnh báo tồn kho từ VwLowStockAlert — ngưỡng lấy từ ProductWarehousePolicy
            var rawAlerts = await _dashboardRepository.GetLowStockAlertsAsync(10);
            var alerts = new List<LowStockAlertDto>();
            int lowStockCount = 0;
            int outOfStockCount = 0;

            foreach (var item in rawAlerts)
            {
                var available = item.AvailableQuantity ?? 0m;

                string status;
                if (available <= 0)
                {
                    status = "Hết hàng";
                    outOfStockCount++;
                }
                else
                {
                    // Sản phẩm có trong view này đã là dưới ngưỡng MinimumStockQuantity
                    status = "Sắp hết";
                    lowStockCount++;
                }

                alerts.Add(new LowStockAlertDto
                {
                    ProductId = item.ProductId,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    AvailableQuantity = available,
                    UnitName = string.Empty, // View chưa có UnitName, có thể mở rộng sau
                    Threshold = item.MinimumStockQuantity,
                    ShortageQuantity = item.ShortageQuantity ?? 0m,
                    Status = status
                });
            }

            var rawActivities = await _dashboardRepository.GetRecentActivitiesAsync(5);
            var activities = rawActivities.Select(a => new RecentActivityDto
            {
                Code = $"TXN-{a.InventoryTransactionId}",
                Title = $"Giao dịch {a.TransactionType} cho sản phẩm {a.Product?.ProductName ?? "sản phẩm"}",
                PerformerName = a.PerformedByUser?.FullName ?? "Hệ thống",
                TimeAgo = GetTimeAgo(a.TransactionAt),
                StatusType = a.TransactionType == "INBOUND" ? "Success" : "Warning"
            }).ToList();

            if (activities.Count == 0)
            {
                activities = new List<RecentActivityDto>
                {
                    new() { Title = $"Tổng quan kho: {totalProducts} mặt hàng đang được quản lý", PerformerName = "Hệ thống", TimeAgo = "Vừa xong", StatusType = "Success" },
                    new() { Title = $"Có {pendingPO} đơn nhập chờ xử lý và {pendingSO} đơn xuất đang chờ giữ tồn", PerformerName = "Hệ thống", TimeAgo = "Hôm nay", StatusType = "Warning" },
                    new() { Title = $"{lowStockCount} mục đang ở ngưỡng sắp hết và {outOfStockCount} mục đã hết hàng", PerformerName = "Hệ thống", TimeAgo = "Hôm nay", StatusType = "Warning" }
                };
            }

            return new DashboardSummaryDto
            {
                TotalProducts = totalProducts,
                ActiveWarehouses = activeWarehouses,
                LowStockCount = lowStockCount,
                OutOfStockCount = outOfStockCount,

                PendingPurchaseOrders = pendingPO,
                PendingSalesOrders = pendingSO,
                ProcessingInboundOrders = processingInbound,
                PickingOutboundOrders = pickingOutbound,

                LowStockAlerts = alerts.Take(5).ToList(),
                RecentActivities = activities
            };
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Vừa xong";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giờ trước";

            return $"{(int)timeSpan.TotalDays} ngày trước";
        }
    }
}
