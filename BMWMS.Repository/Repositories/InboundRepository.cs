using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;
using BMWMS.Repository.Interfaces;

namespace BMWMS.Repository.Repositories;

public class InboundRepository : IInboundRepository
{
    private readonly BmwmsContext _context;

    public InboundRepository(BmwmsContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<InboundOrder> Items, int TotalCount)> GetInboundOrdersPageAsync(
        string? keyword,
        string? status,
        string? sourceType,
        DateTime? fromDate,
        DateTime? toDate,
        long? assignedToUserId,
        int pageIndex,
        int pageSize)
    {
        var query = _context.InboundOrders
            .Include(x => x.PurchaseOrder)
                .ThenInclude(p => p.Supplier)
            .Include(x => x.SalesOrder)
                .ThenInclude(s => s.Customer)
            .Include(x => x.InboundOrderItems)
                .ThenInclude(x => x.InboundOrderDetails)
                    .ThenInclude(x => x.InventoryTransaction)
            .AsNoTracking()
            .AsQueryable();

        if (assignedToUserId.HasValue)
        {
            query = query.Where(x => x.AssignedToUserId == assignedToUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(sourceType))
        {
            var normalizedSourceType = sourceType.Trim().ToUpperInvariant();
            query = query.Where(x => x.SourceType == normalizedSourceType);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var lowerKeyword = keyword.ToLower();
            query = query.Where(x => 
                x.InboundOrderNumber.ToLower().Contains(lowerKeyword) ||
                (x.PurchaseOrder != null && x.PurchaseOrder.PurchaseOrderNumber.ToLower().Contains(lowerKeyword)) ||
                (x.PurchaseOrder != null && x.PurchaseOrder.Supplier != null && x.PurchaseOrder.Supplier.SupplierName.ToLower().Contains(lowerKeyword)) ||
                (x.SalesOrder != null && x.SalesOrder.Customer.CustomerName.ToLower().Contains(lowerKeyword)) ||
                (x.SalesOrder != null && x.SalesOrder.SalesOrderNumber.ToLower().Contains(lowerKeyword))
            );
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            status = status.Trim().ToUpperInvariant();
            query = status switch
            {
                "READY" => query.Where(x => x.Status == "ASSIGNED"),
                "RECEIVING" => query.Where(x => x.Status == "IN_PROGRESS"),
                "RECEIVED" => query.Where(x => x.Status == "COMPLETED" &&
                    x.InboundOrderItems.SelectMany(i => i.InboundOrderDetails)
                        .Any(d => d.ConditionStatus == "GOOD" && d.InventoryTransaction == null)),
                "PUTAWAY_COMPLETED" => query.Where(x => x.Status == "COMPLETED" &&
                    !x.InboundOrderItems.SelectMany(i => i.InboundOrderDetails)
                        .Any(d => d.ConditionStatus == "GOOD" && d.InventoryTransaction == null)),
                _ => query.Where(x => x.Status == status)
            };
        }

        if (fromDate.HasValue)
        {
            var from = DateOnly.FromDateTime(fromDate.Value.Date);
            query = query.Where(x => x.ExpectedReceiptDate >= from);
        }

        if (toDate.HasValue)
        {
            var to = DateOnly.FromDateTime(toDate.Value.Date);
            query = query.Where(x => x.ExpectedReceiptDate <= to);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.ExpectedReceiptDate)
            .ThenByDescending(x => x.InboundOrderNumber)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<InboundOrder?> GetByIdAsync(long id)
    {
        return await _context.InboundOrders
            .Include(i => i.PurchaseOrder)
                .ThenInclude(p => p.Supplier)
            .Include(i => i.SalesOrder)
                .ThenInclude(s => s.Customer)
            .Include(i => i.Warehouse)
            .Include(i => i.AssignedToUser)
            .Include(i => i.CreatedByUser)
            .Include(i => i.ConfirmedByUser)
            .Include(i => i.CancelledByUser)
            .Include(i => i.ParentInboundOrder)
            .Include(i => i.InverseParentInboundOrder)
                .ThenInclude(child => child.InboundOrderItems)
            .Include(i => i.InboundOrderItems)
                .ThenInclude(item => item.Product)
                    .ThenInclude(product => product.UnitOfMeasure)
            .Include(i => i.InboundOrderItems)
                .ThenInclude(item => item.InboundOrderDetails)
                    .ThenInclude(detail => detail.ProductLot)
            .Include(i => i.InboundOrderItems)
                .ThenInclude(item => item.InboundOrderDetails)
                    .ThenInclude(detail => detail.StorageLocation)
                        .ThenInclude(location => location.StorageRack)
                            .ThenInclude(rack => rack!.WarehouseZone)
            .Include(i => i.InboundOrderItems)
                .ThenInclude(item => item.InboundOrderDetails)
                    .ThenInclude(detail => detail.RecordedByUser)
            .Include(i => i.InboundOrderItems)
                .ThenInclude(item => item.InboundOrderDetails)
                    .ThenInclude(detail => detail.InventoryTransaction)
                        .ThenInclude(transaction => transaction!.PerformedByUser)
            .FirstOrDefaultAsync(i => i.InboundOrderId == id);
    }

    public async Task AddAsync(InboundOrder inboundOrder)
    {
        await _context.InboundOrders.AddAsync(inboundOrder);
    }

    public Task UpdateAsync(InboundOrder inboundOrder)
    {
        _context.InboundOrders.Update(inboundOrder);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
