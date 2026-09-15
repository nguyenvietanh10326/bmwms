using BMWMS.Repository.Interfaces.Inventory;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static BMWMS.Repository.Interfaces.Inventory.IOutboundOrderRepository;

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
                .Include(o => o.CreatedByUser)
                .Include(o => o.ApprovedByUser)
                .Include(o => o.SalesOrder)
                    .ThenInclude(s => s!.Customer)
                .Include(o => o.PurchaseOrder)
                    .ThenInclude(p => p!.Supplier)
                .Include(o => o.OutboundOrderItems)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.UnitOfMeasure)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(o => o.OutboundOrderNumber.ToLower().Contains(search)
                                      || (o.SalesOrder != null && o.SalesOrder.SalesOrderNumber.ToLower().Contains(search))
                                      || (o.PurchaseOrder != null && o.PurchaseOrder.PurchaseOrderNumber.ToLower().Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var dbStatus = status.Trim().ToUpperInvariant() switch
                {
                    "READY" => "ASSIGNED",
                    "ISSUING" => "IN_PROGRESS",
                    "ISSUED" => "COMPLETED",
                    var value => value
                };
                query = query.Where(o => o.Status == dbStatus);
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
                .Include(o => o.CreatedByUser)
                .Include(o => o.ApprovedByUser)
                .Include(o => o.SalesOrder)
                    .ThenInclude(s => s!.Customer)
                .Include(o => o.PurchaseOrder)
                    .ThenInclude(p => p!.Supplier)
                .Include(o => o.OutboundOrderItems)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p!.UnitOfMeasure)
                .Include(o => o.OutboundOrderItems)
                    .ThenInclude(i => i.OutboundOrderDetails)
                        .ThenInclude(d => d.StorageLocation)
                            .ThenInclude(location => location.StorageRack)
                                .ThenInclude(rack => rack!.WarehouseZone)
                .Include(o => o.OutboundOrderItems)
                    .ThenInclude(i => i.OutboundOrderDetails)
                        .ThenInclude(d => d.ProductLot)
                .Include(o => o.OutboundOrderItems)
                    .ThenInclude(i => i.OutboundOrderDetails)
                        .ThenInclude(d => d.RecordedByUser)
                .Include(o => o.OutboundOrderItems)
                    .ThenInclude(i => i.OutboundOrderDetails)
                        .ThenInclude(d => d.InventoryTransaction)
                .FirstOrDefaultAsync(o => o.OutboundOrderId == id);
        }

    }
}
