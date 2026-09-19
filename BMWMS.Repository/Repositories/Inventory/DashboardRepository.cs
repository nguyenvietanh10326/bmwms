using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Repositories.Inventory
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly BmwmsContext _context; 

        public DashboardRepository(BmwmsContext context)
        {
            _context = context;
        }

        // 1. Tổng số sản phẩm
        public async Task<int> GetTotalProductsCountAsync()
        {
            return await _context.Products.AsNoTracking().CountAsync();
        }

        // 2. Kho đang hoạt động
        public async Task<int> GetActiveWarehousesCountAsync()
        {
            return await _context.Warehouses
                .AsNoTracking()
                .Where(w => w.Status == "ACTIVE")
                .CountAsync();
        }

        // 3. Lấy danh sách cảnh báo tồn kho thấp từ view VwLowStockAlert
        public async Task<List<VwLowStockAlert>> GetLowStockAlertsAsync(int top = 10)
        {
            return await _context.VwLowStockAlerts
                .AsNoTracking()
                .OrderBy(v => v.AvailableQuantity - v.MinimumStockQuantity) // thiếu nhiều nhất lên trên
                .Take(top)
                .ToListAsync();
        }

        // 4. Lấy danh sách Hoạt động/Giao dịch gần đây nhất
        public async Task<List<InventoryTransaction>> GetRecentActivitiesAsync(int top = 5)
        {
            return await _context.InventoryTransactions
                .AsNoTracking()
                .Include(t => t.Product)
                .Include(t => t.PerformedByUser)
                .OrderByDescending(t => t.TransactionAt)
                .Take(top)
                .ToListAsync();
        }

        // 5. Đếm Đơn mua đang chờ xử lý (có trạng thái chưa hoàn tất)
        public async Task<int> GetPendingPurchaseOrdersCountAsync()
        {
            return await _context.PurchaseOrders
                .AsNoTracking()
                .Where(po => po.Status == "DRAFT" ||
                             po.Status == "PENDING_CONFIRMATION" ||
                             po.Status == "CONFIRMED" ||
                             po.Status == "PARTIALLY_RECEIVED")
                .CountAsync();
        }

        // 6. Đếm Đơn bán chờ giữ tồn
        public async Task<int> GetPendingSalesOrdersCountAsync()
        {
            return await _context.SalesOrders
                .AsNoTracking()
                .Where(so => so.Status == "WAITING_STOCK")
                .CountAsync();
        }

        // 7. Lệnh nhập đang xử lý (IN_PROGRESS hoặc ASSIGNED)
        public async Task<int> GetProcessingInboundOrdersCountAsync()
        {
            return await _context.InboundOrders
                .AsNoTracking()
                .Where(io => io.Status == "IN_PROGRESS" || io.Status == "ASSIGNED")
                .CountAsync();
        }

        // 8. Lệnh xuất đang picking
        public async Task<int> GetPickingOutboundOrdersCountAsync()
        {
            return await _context.OutboundOrders
                .AsNoTracking()
                .Where(oo => oo.Status == "IN_PROGRESS")
                .CountAsync();
        }
    }
}
