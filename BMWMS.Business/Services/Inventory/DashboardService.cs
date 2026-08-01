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

            var rawInventories = await _dashboardRepository.GetLowStockInventoriesAsync(10);
            var alerts = new List<LowStockAlertDto>();
            int lowStockCount = 0;
            int outOfStockCount = 0;

            foreach (var item in rawInventories)
            {
                var available = item.AvailableQuantity ?? (item.OnHandQuantity - item.ReservedQuantity);

                decimal threshold = 100;

                string status;
                if (available <= 0)
                {
                    status = "Hết hàng";
                    outOfStockCount++;
                }
                else if (available < threshold)
                {
                    status = "Sắp hết";
                    lowStockCount++;
                }
                else
                {
                    status = "Theo dõi";
                }

                alerts.Add(new LowStockAlertDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product?.ProductName ?? "N/A",
                    AvailableQuantity = available,
                    UnitName = item.Product?.UnitOfMeasure?.UnitName ?? "Đơn vị",
                    Threshold = threshold,
                    Status = status
                });
            }

            var rawActivities = await _dashboardRepository.GetRecentActivitiesAsync(5);
            var activities = rawActivities.Select(a => new RecentActivityDto
            {
                Code = $"TXN-{a.InventoryTransactionId}",
                Title = $"Giao dịch {a.TransactionType} cho sản phẩm {a.Product?.ProductName}",
                PerformerName = a.PerformedByUser?.FullName ?? "Hệ thống",
                TimeAgo = GetTimeAgo(a.TransactionAt),
                StatusType = a.TransactionType == "INBOUND" ? "Success" : "Warning"
            }).ToList();

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
