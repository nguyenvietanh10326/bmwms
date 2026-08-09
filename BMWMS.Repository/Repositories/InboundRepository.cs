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
        DateTime? fromDate,
        DateTime? toDate,
        int pageIndex,
        int pageSize)
    {
        var query = _context.InboundOrders
            .Include(x => x.PurchaseOrder)
                .ThenInclude(p => p.Supplier)
            .Include(x => x.InboundOrderItems)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var lowerKeyword = keyword.ToLower();
            query = query.Where(x => 
                x.InboundOrderNumber.ToLower().Contains(lowerKeyword) ||
                (x.PurchaseOrder != null && x.PurchaseOrder.Supplier != null && x.PurchaseOrder.Supplier.SupplierName.ToLower().Contains(lowerKeyword))
            );
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "All")
        {
            query = query.Where(x => x.Status == status);
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
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
