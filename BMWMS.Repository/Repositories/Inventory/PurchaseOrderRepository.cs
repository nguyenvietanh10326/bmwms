using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Repository.Repositories.Inventory
{
    public class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly BmwmsContext _context; 

        public PurchaseOrderRepository(BmwmsContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<PurchaseOrder> Items, int TotalCount)> GetPagedListAsync(
            string? searchTerm,
            string? status,
            long? warehouseId,
            int pageIndex,
            int pageSize)
        {
            var query = _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.PurchaseOrderDetails)
                    .ThenInclude(pod => pod.Product)
                .Include(po => po.InboundOrders)
                .AsNoTracking()
                .AsQueryable();

            // 1. Lọc theo Từ khóa (Mã PO hoặc Tên/Mã Nhà cung cấp)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var keyword = searchTerm.Trim().ToLower();
                query = query.Where(po =>
                    po.PurchaseOrderNumber.ToLower().Contains(keyword) ||
                    po.Supplier.SupplierName.ToLower().Contains(keyword) ||
                    po.Supplier.SupplierCode.ToLower().Contains(keyword));
            }

            // 2. Lọc theo Trạng thái PO
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(po => po.Status == status);
            }

            // 3. Lọc theo Kho dự kiến (Thông qua InboundOrder liên kết)
            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                query = query.Where(po => po.InboundOrders.Any(io => io.WarehouseId == warehouseId.Value));
            }

            // Đếm tổng số bản ghi trước khi phân trang
            int totalCount = await query.CountAsync();

            // Phân trang & Sắp xếp mới nhất lên đầu
            var items = await query
                .OrderByDescending(po => po.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<PurchaseOrder?> GetByIdWithDetailsAsync(long purchaseOrderId)
        {
            return await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.CreatedByUser)
                .Include(po => po.ConfirmedByUser)
                .Include(po => po.PurchaseOrderDetails)
                    .ThenInclude(pod => pod.Product)
                        .ThenInclude(p => p.UnitOfMeasure)
                .Include(po => po.InboundOrders)
                    .ThenInclude(io => io.Warehouse)
                .Include(po => po.InboundOrders)
                    .ThenInclude(io => io.InboundOrderItems)
                        .ThenInclude(item => item.InboundOrderDetails)
                            .ThenInclude(detail => detail.InventoryTransaction)
                .Include(po => po.InboundOrders)
                    .ThenInclude(io => io.AssignedToUser)
                .FirstOrDefaultAsync(po => po.PurchaseOrderId == purchaseOrderId);
        }

        public async Task<string> GeneratePurchaseOrderNumberAsync()
        {
            int currentYear = DateTime.Now.Year;
            string prefix = $"PO-{currentYear}-";

            // Tải tất cả mã PO bắt đầu bằng prefix trong năm hiện tại
            var allPos = await _context.PurchaseOrders
                .Where(po => po.PurchaseOrderNumber.StartsWith(prefix))
                .Select(po => po.PurchaseOrderNumber)
                .ToListAsync();

            int maxSeq = 0;
            foreach (var num in allPos)
            {
                if (num.Length > prefix.Length)
                {
                    string seqStr = num.Substring(prefix.Length);
                    if (int.TryParse(seqStr, out int seq))
                    {
                        if (seq > maxSeq)
                        {
                            maxSeq = seq;
                        }
                    }
                }
            }

            int nextSequence = maxSeq + 1;
            return $"{prefix}{nextSequence:D4}";
        }

        public async Task<PurchaseOrder> AddAsync(PurchaseOrder entity)
        {
            await _context.PurchaseOrders.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(PurchaseOrder entity)
        {
            _context.PurchaseOrders.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long purchaseOrderId)
        {
            var entity = await _context.PurchaseOrders.FindAsync(purchaseOrderId);
            if (entity != null)
            {
                _context.PurchaseOrders.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(long purchaseOrderId)
        {
            return await _context.PurchaseOrders.AnyAsync(po => po.PurchaseOrderId == purchaseOrderId);
        }

        public async Task<IEnumerable<Supplier>> GetAllAsync()
        {
            return await _context.Suppliers
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Warehouse>> GetAllWareAsync()
        {
            return await _context.Warehouses
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetAllProductAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .Include(a => a.UnitOfMeasure)
                .Include(a => a.ProductGroup)
                .Include(a => a.SupplierProducts)
                .Include(a => a.Inventories)
                .Where(p => p.Status == "ACTIVE" && p.ProductGroup.Status == "ACTIVE")
                .ToListAsync();
        }

        public async Task<IEnumerable<Customer>> GetCustomersAsync()
        {
            return await _context.Customers
                .AsNoTracking()
                .Where(c => c.Status == "ACTIVE")
                .OrderBy(c => c.CustomerName)
                .ToListAsync();
        }
    }
}
