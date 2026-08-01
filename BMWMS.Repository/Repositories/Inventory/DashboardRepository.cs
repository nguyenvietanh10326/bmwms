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
                .Where(w => w.Status == "ACTIVE" || w.Status == "Active")
                .CountAsync();
        }

        // 3. Lấy danh sách Tồn kho kèm Sản phẩm & Đơn vị tính để phục vụ bảng Cảnh báo tồn kho
        public async Task<List<BMWMS.Repository.Models.Inventory>> GetLowStockInventoriesAsync(int top = 5)
        {
            return await _context.Inventories
                .AsNoTracking()
                .Include(i => i.Product)
                    .ThenInclude(p => p.UnitOfMeasure)
                .Include(i => i.StorageLocation)
                .OrderBy(i => (i.AvailableQuantity ?? (i.OnHandQuantity - i.ReservedQuantity)))
                .Take(top)
                .ToListAsync();
        }

        // 4. Lấy danh sách Hoạt động/Giao dịch gần đây nhất
        public async Task<List<BMWMS.Repository.Models.InventoryTransaction>> GetRecentActivitiesAsync(int top = 5)
        {
            return await _context.InventoryTransactions
                .AsNoTracking()
                .Include(t => t.Product)
                .Include(t => t.PerformedByUser)
                .OrderByDescending(t => t.TransactionAt)
                .Take(top)
                .ToListAsync();
        }

        // 5. Đếm Đơn mua chờ xác nhận
        public async Task<int> GetPendingPurchaseOrdersCountAsync()
        {
            return await _context.PurchaseOrderDetails
                .AsNoTracking()
                .Select(po => po.PurchaseOrderId)
                .Distinct()
                .CountAsync(); 
        }

        // 6. Đếm Đơn bán chờ giữ tồn
        public async Task<int> GetPendingSalesOrdersCountAsync()
        {
            return await _context.SalesOrderDetails
                .AsNoTracking()
                .Select(so => so.SalesOrderId)
                .Distinct()
                .CountAsync();
        }

        // 7. Lệnh nhập đang xử lý
        public async Task<int> GetProcessingInboundOrdersCountAsync()
        {
            return await _context.InboundOrderItems
                .AsNoTracking()
                .Select(i => i.InboundOrderId)
                .Distinct()
                .CountAsync();
        }

        // 8. Lệnh xuất đang picking
        public async Task<int> GetPickingOutboundOrdersCountAsync()
        {
            return await _context.OutboundOrderItems
                .AsNoTracking()
                .Select(o => o.OutboundOrderId)
                .Distinct()
                .CountAsync();
        }
    }
}
