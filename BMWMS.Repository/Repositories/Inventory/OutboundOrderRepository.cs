using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BMWMS.Repository.Repositories.Inventory
{
    public class OutboundOrderRepository : IOutboundOrderRepository
    {
        private readonly BmwmsContext _context;

        public OutboundOrderRepository(BmwmsContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<OutboundOrder>> GetAllAsync(string? search, string? status, long? warehouseId)
        {
            var query = _context.OutboundOrders
                .Include(o => o.Warehouse)
                .Include(o => o.AssignedToUser)
                .Include(o => o.SalesOrder)
                    .ThenInclude(s => s!.Customer)
                .Include(o => o.OutboundOrderItems)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.UnitOfMeasure)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(o => o.OutboundOrderNumber.ToLower().Contains(search)
                                      || (o.SalesOrder != null && o.SalesOrder.SalesOrderNumber.ToLower().Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            if (warehouseId.HasValue && warehouseId > 0)
            {
                query = query.Where(o => o.WarehouseId == warehouseId);
            }

            return await query.OrderByDescending(o => o.OutboundOrderId).ToListAsync();
        }

        public async Task<OutboundOrder?> GetByIdAsync(long id)
        {
            return await _context.OutboundOrders
                .Include(o => o.Warehouse)
                .Include(o => o.AssignedToUser)
                .Include(o => o.SalesOrder)
                    .ThenInclude(s => s!.Customer)
                .Include(o => o.OutboundOrderItems)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.UnitOfMeasure)
                .FirstOrDefaultAsync(o => o.OutboundOrderId == id);
        }

        public async Task<OutboundOrder?> GetByOrderNumberAsync(string orderNumber)
        {
            return await _context.OutboundOrders
                .Include(o => o.Warehouse)
                .Include(o => o.AssignedToUser)
                .Include(o => o.SalesOrder)
                    .ThenInclude(s => s!.Customer)
                .Include(o => o.OutboundOrderItems)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.UnitOfMeasure)
                .FirstOrDefaultAsync(o => o.OutboundOrderNumber == orderNumber);
        }

        public async Task<OutboundOrder> CreateAsync(OutboundOrder outboundOrder)
        {
            _context.OutboundOrders.Add(outboundOrder);
            await _context.SaveChangesAsync();
            return outboundOrder;
        }

        public async Task<bool> UpdateStatusAsync(long outboundOrderId, string status)
        {
            var order = await _context.OutboundOrders.FindAsync(outboundOrderId);
            if (order == null) return false;

            order.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(long id)
        {
            return await _context.OutboundOrders.AnyAsync(o => o.OutboundOrderId == id);
        }
    }
}