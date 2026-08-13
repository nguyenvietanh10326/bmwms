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

    }
}
