using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Repositories.Inventory
{
    public class SalesOrderRepository : ISalesOrderRepository
    {
        private readonly BmwmsContext _context;

        public SalesOrderRepository(BmwmsContext context)
        {
            _context = context;
        }


        public async Task<OutboundOrder?> GetOutboundOrderDetailAsync(
            long outboundOrderId)
        {
            return await _context.OutboundOrders
                .Include(x => x.Warehouse)
                .Include(x => x.SalesOrder)
                    .ThenInclude(x => x!.Customer)
                .Include(x => x.SalesOrder)
                    .ThenInclude(x => x!.SalesOrderDetails)
                        .ThenInclude(x => x.Product)
                            .ThenInclude(x => x.UnitOfMeasure)
                .Include(x => x.OutboundOrderItems)

                .FirstOrDefaultAsync(x =>
                    x.OutboundOrderId == outboundOrderId);
        }


        public async Task<List<User>> GetSalesOrderCreatorsAsync(
    CancellationToken cancellationToken = default)
        {
            return await _context.SalesOrders
                .AsNoTracking()
                .Where(x => x.CreatedByUser != null)
                .Select(x => x.CreatedByUser)
                .Distinct()
                .OrderBy(x => x.FullName)
                .ToListAsync(cancellationToken);
        }
        public async Task<List<SalesOrder>> GetConfirmedSalesOrdersAsync()
        {
            return await _context.SalesOrders
                .AsNoTracking()
                .Where(so => so.Status == "CONFIRMED" || so.Status == "APPROVED")
                .ToListAsync();
        }

        public async Task<decimal> GetReservedQuantityAsync(
            long salesOrderDetailId)
        {
            return await _context.InventoryReservations
                .Where(r =>
                    r.SalesOrderDetailId == salesOrderDetailId &&
                    r.Status == "RESERVED")
                .SumAsync(r => (decimal?)r.ReservedQuantity) ?? 0;
        }
        public async Task<List<string>> GetReservedLotBinInfoAsync(
            long salesOrderDetailId)
        {
            return await _context.InventoryReservations
                .Where(r =>
                    r.SalesOrderDetailId == salesOrderDetailId &&
                    r.Status == "RESERVED")
                .Select(r =>
                    $"{r.ProductLot.LotNumber} · {r.StorageLocation.LocationCode}")
                .ToListAsync();
        }
        public async Task<(IEnumerable<SalesOrder> Items, int TotalCount)> GetPagedAsync(
            string? keyword,
            string? status,
            int pageIndex,
            int pageSize)
        {
            var query = _context.SalesOrders
                .Include(so => so.Customer)
                .Include(so => so.CreatedByUser)
                .Include(so => so.ConfirmedByUser)
                .Include(so => so.SalesOrderDetails)
                    .ThenInclude(d => d.Product)
                        .ThenInclude(p => p.UnitOfMeasure)
                .AsNoTracking()
                .AsQueryable();

            // Tìm kiếm theo Mã SO hoặc Khách hàng (Tên / Mã)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(x => x.SalesOrderNumber.ToLower().Contains(kw) ||
                                         x.Customer.CustomerName.ToLower().Contains(kw) ||
                                         x.Customer.CustomerCode.ToLower().Contains(kw));
            }

            // Lọc theo Trạng thái
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Status == status);
            }

            int totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<SalesOrder?> GetByIdAsync(long salesOrderId)
        {
            return await _context.SalesOrders
                .Include(x => x.Customer)
                .Include(x => x.CreatedByUser)
                .Include(x => x.ConfirmedByUser)
                .Include(x => x.SalesOrderDetails)
                    .ThenInclude(d => d.Product)
                        .ThenInclude(p => p.UnitOfMeasure)
                .Include(x => x.OutboundOrders)
                .FirstOrDefaultAsync(x => x.SalesOrderId == salesOrderId);
        }

        public async Task<SalesOrder?> GetByNumberAsync(string salesOrderNumber)
        {
            return await _context.SalesOrders
                .Include(x => x.Customer)
                .Include(x => x.SalesOrderDetails)
                .FirstOrDefaultAsync(x => x.SalesOrderNumber == salesOrderNumber);
        }

        public async Task<SalesOrder> CreateAsync(SalesOrder salesOrder)
        {
            salesOrder.CreatedAt = DateTime.Now;
            if (string.IsNullOrEmpty(salesOrder.Status))
            {
                salesOrder.Status = "Nháp";
            }

            _context.SalesOrders.Add(salesOrder);
            await _context.SaveChangesAsync();
            return salesOrder;
        }

        public async Task<bool> UpdateAsync(SalesOrder salesOrder)
        {
            var existing = await _context.SalesOrders
                .Include(x => x.SalesOrderDetails)
                .FirstOrDefaultAsync(x => x.SalesOrderId == salesOrder.SalesOrderId);

            if (existing == null) return false;

            // Update thông tin master
            existing.CustomerId = salesOrder.CustomerId;
            existing.OrderDate = salesOrder.OrderDate;
            existing.ExpectedIssueDate = salesOrder.ExpectedIssueDate;
            existing.Notes = salesOrder.Notes;
            existing.UpdatedAt = DateTime.Now;

            // Update Details (Xóa các item cũ, gán danh sách mới)
            _context.SalesOrderDetails.RemoveRange(existing.SalesOrderDetails);
            existing.SalesOrderDetails = salesOrder.SalesOrderDetails;

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateStatusAsync(long salesOrderId, string status, long? confirmedByUserId = null)
        {
            var existing = await _context.SalesOrders.FindAsync(salesOrderId);
            if (existing == null) return false;

            existing.Status = status;
            existing.UpdatedAt = DateTime.Now;

            if (confirmedByUserId.HasValue)
            {
                existing.ConfirmedByUserId = confirmedByUserId;
                existing.ConfirmedAt = DateTime.Now;
            }

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<string> GenerateSalesOrderNumberAsync()
        {
            int currentYear = DateTime.Now.Year;
            string prefix = $"SO-{currentYear}-";

            var lastOrder = await _context.SalesOrders
                .Where(x => x.SalesOrderNumber.StartsWith(prefix))
                .OrderByDescending(x => x.SalesOrderNumber)
                .FirstOrDefaultAsync();

            if (lastOrder == null)
            {
                return $"{prefix}0001";
            }

            string lastNumberStr = lastOrder.SalesOrderNumber.Replace(prefix, "");
            if (int.TryParse(lastNumberStr, out int lastNum))
            {
                return $"{prefix}{(lastNum + 1):D4}";
            }

            return $"{prefix}{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
        }

    }
}
