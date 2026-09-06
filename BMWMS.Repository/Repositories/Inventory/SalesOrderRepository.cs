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
                .Where(so => (so.Status == "CONFIRMED" || so.Status == "APPROVED" ||
                              so.Status == "ALLOCATED" || so.Status == "PARTIALLY_FULFILLED") &&
                             so.SalesOrderDetails.Any(d =>
                                 d.OrderedQuantity > d.FulfilledQuantity +
                                 so.OutboundOrders
                                     .Where(o => o.Status == "DRAFT" || o.Status == "ASSIGNED" || o.Status == "IN_PROGRESS")
                                     .SelectMany(o => o.OutboundOrderItems)
                                     .Where(i => i.ProductId == d.ProductId)
                                     .Sum(i => i.RequestedQuantity - i.IssuedQuantity)))
                .OrderByDescending(so => so.SalesOrderId)
                .ToListAsync();
        }

        public async Task<Customer?> GetActiveCustomerAsync(long customerId)
        {
            return await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Status == "ACTIVE");
        }

        public async Task<List<Product>> GetActiveProductsAsync(IEnumerable<long> productIds)
        {
            var ids = productIds.Distinct().ToList();
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.ProductGroup)
                .Include(p => p.UnitOfMeasure)
                .Where(p => ids.Contains(p.ProductId) &&
                            p.Status == "ACTIVE" &&
                            p.ProductGroup.Status == "ACTIVE")
                .ToListAsync();
        }

        public async Task<List<Product>> GetActiveProductsForLookupAsync()
        {
            return await _context.Products
                .AsNoTracking()
                .Include(p => p.UnitOfMeasure)
                .Include(p => p.ProductGroup)
                .Include(p => p.Inventories)
                .Where(p => p.Status == "ACTIVE" && p.ProductGroup.Status == "ACTIVE")
                .OrderBy(p => p.ProductCode)
                .ToListAsync();
        }

        public async Task<decimal> GetReservedQuantityAsync(
            long salesOrderDetailId)
        {
            return await _context.InventoryReservations
                .Where(r =>
                    r.SalesOrderDetailId == salesOrderDetailId &&
                    (r.Status == "ACTIVE" || r.Status == "PARTIALLY_CONSUMED"))
                .SumAsync(r => (decimal?)(r.ReservedQuantity - r.ConsumedQuantity)) ?? 0;
        }
        public async Task<List<string>> GetReservedLotBinInfoAsync(
            long salesOrderDetailId)
        {
            return await _context.InventoryReservations
                .Where(r =>
                    r.SalesOrderDetailId == salesOrderDetailId &&
                    (r.Status == "ACTIVE" || r.Status == "PARTIALLY_CONSUMED"))
                .Select(r =>
                    $"{r.ProductLot.LotNumber} - {r.StorageLocation.LocationCode}")
                .ToListAsync();
        }
        public async Task<(IEnumerable<SalesOrder> Items, int TotalCount)> GetPagedAsync(
            string? keyword,
            string? status,
            DateOnly? fromDate,
            DateOnly? toDate,
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
                query = status switch
                {
                    // Hỗ trợ đọc dữ liệu cũ trong giai đoạn chuyển sang state machine Report 3.1.
                    "CONFIRMED" => query.Where(x => x.Status == "CONFIRMED" || x.Status == "ALLOCATED"),
                    "PARTIALLY_ISSUED" => query.Where(x => x.Status == "PARTIALLY_ISSUED" || x.Status == "PARTIALLY_FULFILLED"),
                    "ISSUED" => query.Where(x => x.Status == "ISSUED" || x.Status == "FULFILLED" || x.Status == "CLOSED"),
                    _ => query.Where(x => x.Status == status)
                };
            }

            if (fromDate.HasValue)
            {
                query = query.Where(x => x.OrderDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => x.OrderDate <= toDate.Value);
            }

            int totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.OrderDate)
                .ThenByDescending(x => x.SalesOrderNumber)
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

        public async Task<(IEnumerable<SalesOrder> Items, int TotalCount)> GetPagedListAsync(
            string? searchTerm, string? status, long? warehouseId, int pageIndex, int pageSize)
        {
            var query = _context.SalesOrders
                .Include(so => so.Customer)
                .Include(so => so.SalesOrderDetails)
                    .ThenInclude(d => d.Product)
                        .ThenInclude(p => p.UnitOfMeasure)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(so => so.SalesOrderNumber.Contains(searchTerm) || 
                                          (so.Customer != null && so.Customer.CustomerName.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(so => so.Status == status);
            }

            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                // SO list filtering by warehouse via OutboundOrders
                query = query.Where(so => so.OutboundOrders.Any(o => o.WarehouseId == warehouseId.Value));
            }

            int totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(so => so.CreatedAt)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<SalesOrder?> GetByIdWithDetailsAsync(long salesOrderId)
        {
            return await _context.SalesOrders
                .Include(so => so.Customer)
                .Include(so => so.CreatedByUser)
                .Include(so => so.ConfirmedByUser)
                .Include(so => so.OutboundOrders)
                    .ThenInclude(o => o.Warehouse)
                .Include(so => so.SalesOrderDetails)
                    .ThenInclude(d => d.Product)
                        .ThenInclude(p => p.UnitOfMeasure)
                .FirstOrDefaultAsync(so => so.SalesOrderId == salesOrderId);
        }
        public async Task AddAsync(SalesOrder entity)
        {
            await _context.SalesOrders.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<Customer?> GetCustomerByPhoneAsync(string phone)
        {
            return await _context.Customers.FirstOrDefaultAsync(c => c.PhoneNumber == phone);
        }

        public async Task<Customer> AddCustomerAsync(Customer customer)
        {
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
            return customer;
        }
    }
}
