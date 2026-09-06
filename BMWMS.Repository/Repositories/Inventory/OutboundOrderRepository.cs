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
        public async Task SavePickDetailAsync(OutboundOrderDetail detail, OutboundOrderItem item, OutboundOrder order)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.OutboundOrderDetails.AddAsync(detail);

                _context.OutboundOrderItems.Update(item);

                _context.OutboundOrders.Update(order);

                if (detail.InventoryReservationId.HasValue && detail.InventoryReservationId > 0)
                {
                    var reservation = await _context.InventoryReservations.FindAsync(detail.InventoryReservationId.Value);
                    if (reservation != null)
                    {
                        reservation.Status = "FULFILLED";
                        _context.InventoryReservations.Update(reservation);
                    }
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<AvailableLocationModel>> GetAvailableLocationsAsync(long warehouseId, long productId)
        {
            var availableItems = await _context.InventoryReservations
                .Where(r => r.StorageLocation.WarehouseId == warehouseId
                         && r.ProductId == productId
                         && (r.Status == "ACTIVE" || r.Status == "PARTIALLY_CONSUMED"))
                .Select(r => new AvailableLocationModel
                {
                    StorageLocationId = r.StorageLocationId,
                    LocationCode = r.StorageLocation.LocationCode,
                    ProductLotId = r.ProductLotId,
                    LotNumber = r.ProductLot.LotNumber,
                    InventoryReservationId = r.InventoryReservationId,
                    AvailableQuantity = r.ReservedQuantity - r.ConsumedQuantity
                })
                .ToListAsync();

            return availableItems;
        }
    }
}
