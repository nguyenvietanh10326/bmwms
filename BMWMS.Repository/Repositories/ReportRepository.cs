using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly BmwmsContext _context;

    public ReportRepository(BmwmsContext context)
    {
        _context = context;
    }

    public async Task<(List<VwInventoryAvailability> Items, int TotalCount)>
        GetInventoryReportAsync(string? keyword, string? locationCode, string? lotNumber, bool positiveStockOnly, int pageIndex, int pageSize)
    {
        var query = _context.Set<VwInventoryAvailability>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(v => v.ProductCode.ToLower().Contains(kw) || v.ProductName.ToLower().Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(locationCode))
        {
            query = query.Where(v => v.LocationCode == locationCode);
        }

        if (!string.IsNullOrWhiteSpace(lotNumber))
        {
            query = query.Where(v => v.LotNumber == lotNumber);
        }

        if (positiveStockOnly)
        {
            query = query.Where(v => v.OnHandQuantity > 0);
        }

        int totalCount = await query.CountAsync();
        
        var items = await query
            .OrderBy(v => v.ProductCode)
            .ThenBy(v => v.LotNumber)
            .ThenBy(v => v.LocationCode)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(int TotalCount, List<VwInboundReport> Items)> GetInboundReportAsync(
        DateTime? fromDate, DateTime? toDate, string? productSearch, string? status, int pageNumber, int pageSize)
    {
        var query = _context.VwInboundReports.AsQueryable();

        if (fromDate.HasValue)
        {
            var fromDateOnly = DateOnly.FromDateTime(fromDate.Value);
            query = query.Where(x => x.ExpectedReceiptDate >= fromDateOnly);
        }

        if (toDate.HasValue)
        {
            var toDateOnly = DateOnly.FromDateTime(toDate.Value);
            query = query.Where(x => x.ExpectedReceiptDate <= toDateOnly);
        }

        if (!string.IsNullOrEmpty(productSearch))
        {
            var search = productSearch.ToLower();
            query = query.Where(x => x.ProductCode.ToLower().Contains(search) || x.ProductName.ToLower().Contains(search));
        }

        if (!string.IsNullOrEmpty(status))
        {
            var statusUpper = status.ToUpper();
            query = query.Where(x => x.Status == statusUpper);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.ExpectedReceiptDate)
            .ThenBy(x => x.InboundOrderNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        await AttachUnitMetadataAsync(items);
        return (totalCount, items);
    }

    public async Task<(int TotalCount, List<VwOutboundReport> Items)> GetOutboundReportAsync(
        DateTime? fromDate, DateTime? toDate, string? productSearch, string? status, int pageNumber, int pageSize)
    {
        var query = _context.VwOutboundReports.AsQueryable();

        if (fromDate.HasValue)
        {
            var fromDateOnly = DateOnly.FromDateTime(fromDate.Value);
            query = query.Where(x => x.ExpectedIssueDate >= fromDateOnly);
        }

        if (toDate.HasValue)
        {
            var toDateOnly = DateOnly.FromDateTime(toDate.Value);
            query = query.Where(x => x.ExpectedIssueDate <= toDateOnly);
        }

        if (!string.IsNullOrEmpty(productSearch))
        {
            var search = productSearch.ToLower();
            query = query.Where(x => x.ProductCode.ToLower().Contains(search) || x.ProductName.ToLower().Contains(search));
        }

        if (!string.IsNullOrEmpty(status))
        {
            var statusUpper = status.ToUpper();
            query = query.Where(x => x.Status == statusUpper);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.ExpectedIssueDate)
            .ThenBy(x => x.OutboundOrderNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        await AttachUnitMetadataAsync(items);
        return (totalCount, items);
    }

    private async Task AttachUnitMetadataAsync(List<VwInboundReport> items)
    {
        var productIds = items.Select(item => item.ProductId).Distinct().ToList();
        if (productIds.Count == 0)
            return;

        var units = await _context.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.ProductId))
            .Select(product => new
            {
                product.ProductId,
                product.UnitOfMeasure.UnitCode
            })
            .ToDictionaryAsync(row => row.ProductId);

        foreach (var item in items)
        {
            if (!units.TryGetValue(item.ProductId, out var unit))
                continue;
            item.UnitCode = unit.UnitCode;
        }
    }

    private async Task AttachUnitMetadataAsync(List<VwOutboundReport> items)
    {
        var productIds = items.Select(item => item.ProductId).Distinct().ToList();
        if (productIds.Count == 0)
            return;

        var units = await _context.Products
            .AsNoTracking()
            .Where(product => productIds.Contains(product.ProductId))
            .Select(product => new
            {
                product.ProductId,
                product.UnitOfMeasure.UnitCode
            })
            .ToDictionaryAsync(row => row.ProductId);

        foreach (var item in items)
        {
            if (!units.TryGetValue(item.ProductId, out var unit))
                continue;
            item.UnitCode = unit.UnitCode;
        }
    }

    public async Task<(int TotalCount, List<(string ProductCode, string ProductName, decimal OpeningBalance, decimal InboundQuantity, decimal OutboundQuantity, decimal AdjustmentQuantity, decimal ClosingBalance)> Items)> GetInOutStockReportAsync(
        DateTime? fromDate, DateTime? toDate, string? productSearch, int pageNumber, int pageSize)
    {
        var query = _context.VwInventoryInOutSummaries.AsQueryable();

        if (toDate.HasValue)
        {
            var toDateOnly = DateOnly.FromDateTime(toDate.Value);
            query = query.Where(x => x.TransactionDate <= toDateOnly);
        }
        
        if (!string.IsNullOrEmpty(productSearch))
        {
            var search = productSearch.ToLower();
            query = query.Where(x => x.ProductCode.ToLower().Contains(search) || x.ProductName.ToLower().Contains(search));
        }

        var fDateOnly = fromDate.HasValue ? DateOnly.FromDateTime(fromDate.Value) : DateOnly.MinValue;

        var groupedQuery = query.GroupBy(x => new { x.ProductCode, x.ProductName })
            .Select(g => new
            {
                ProductCode = g.Key.ProductCode,
                ProductName = g.Key.ProductName,
                OpeningBalance = g.Where(x => x.TransactionDate < fDateOnly).Sum(x => x.NetMovementQuantity) ?? 0,
                InboundQuantity = g.Where(x => x.TransactionDate >= fDateOnly).Sum(x => x.InboundQuantity) ?? 0,
                OutboundQuantity = g.Where(x => x.TransactionDate >= fDateOnly).Sum(x => x.OutboundQuantity) ?? 0,
                AdjustmentQuantity = g.Where(x => x.TransactionDate >= fDateOnly).Sum(x => x.AdjustmentQuantity) ?? 0,
                ClosingBalance = g.Sum(x => x.NetMovementQuantity) ?? 0
            })
            .Where(x => x.OpeningBalance != 0 || x.InboundQuantity != 0 || x.OutboundQuantity != 0 || x.AdjustmentQuantity != 0);

        var totalCount = await groupedQuery.CountAsync();

        var anonymousItems = await groupedQuery
            .OrderBy(x => x.ProductCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = anonymousItems.Select(x => (x.ProductCode, x.ProductName, x.OpeningBalance, x.InboundQuantity, x.OutboundQuantity, x.AdjustmentQuantity, x.ClosingBalance)).ToList();

        return (totalCount, items);
    }

    public async Task<(int TotalCount, List<(long SupplierId, string SupplierCode, string SupplierName, int InboundOrderCount, decimal ExpectedQuantity, decimal ReceivedQuantity, decimal DamagedQuantity, decimal ShortageQuantity)> Items)> GetSupplierStatisticsAsync(
        DateTime? fromDate, DateTime? toDate, string? supplierSearch, int pageNumber, int pageSize)
    {
        var supplierQuery = _context.Suppliers.AsQueryable();

        if (!string.IsNullOrEmpty(supplierSearch))
        {
            var search = supplierSearch.ToLower();
            supplierQuery = supplierQuery.Where(s => s.SupplierCode.ToLower().Contains(search) || s.SupplierName.ToLower().Contains(search));
        }

        var totalCount = await supplierQuery.CountAsync();

        var suppliers = await supplierQuery
            .OrderBy(s => s.SupplierCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new {
                s.SupplierId,
                s.SupplierCode,
                s.SupplierName
            })
            .ToListAsync();

        var supplierIds = suppliers.Select(s => s.SupplierId).ToList();

        var orderQuery = _context.InboundOrders
            .Include(o => o.PurchaseOrder)
            .Include(o => o.InboundOrderItems)
            .Where(o => o.PurchaseOrder != null && supplierIds.Contains(o.PurchaseOrder.SupplierId));

        if (fromDate.HasValue)
        {
            var fDate = DateOnly.FromDateTime(fromDate.Value);
            orderQuery = orderQuery.Where(o => o.ExpectedReceiptDate >= fDate);
        }
        if (toDate.HasValue)
        {
            var tDate = DateOnly.FromDateTime(toDate.Value);
            orderQuery = orderQuery.Where(o => o.ExpectedReceiptDate <= tDate);
        }

        var orders = await orderQuery
            .Select(o => new
            {
                SupplierId = o.PurchaseOrder!.SupplierId,
                o.InboundOrderId,
                ExpectedQuantity = o.InboundOrderItems.Sum(i => i.ExpectedQuantity),
                ReceivedQuantity = o.InboundOrderItems.Sum(i => i.ReceivedQuantity),
                DamagedQuantity = o.InboundOrderItems.Sum(i => i.DamagedQuantity),
                ShortageQuantity = o.InboundOrderItems.Sum(i => i.ShortageQuantity)
            })
            .ToListAsync();

        var groupedOrders = orders.GroupBy(o => o.SupplierId)
            .Select(g => new
            {
                SupplierId = g.Key,
                OrderCount = g.Select(o => o.InboundOrderId).Distinct().Count(),
                ExpectedQuantity = g.Sum(o => o.ExpectedQuantity),
                ReceivedQuantity = g.Sum(o => o.ReceivedQuantity),
                DamagedQuantity = g.Sum(o => o.DamagedQuantity),
                ShortageQuantity = g.Sum(o => o.ShortageQuantity)
            })
            .ToDictionary(x => x.SupplierId);

        var items = new List<(long, string, string, int, decimal, decimal, decimal, decimal)>();

        foreach (var s in suppliers)
        {
            var stat = groupedOrders.GetValueOrDefault(s.SupplierId);
            items.Add((
                s.SupplierId,
                s.SupplierCode,
                s.SupplierName,
                stat?.OrderCount ?? 0,
                stat?.ExpectedQuantity ?? 0,
                stat?.ReceivedQuantity ?? 0,
                stat?.DamagedQuantity ?? 0,
                stat?.ShortageQuantity ?? 0
            ));
        }

        return (totalCount, items);
    }

    public async Task<(int TotalCount, List<VwLowStockAlert> Items)> GetLowStockAlertsAsync(
        string? keyword, int pageNumber, int pageSize)
    {
        var query = _context.Set<VwLowStockAlert>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(v => v.ProductCode.ToLower().Contains(kw) || v.ProductName.ToLower().Contains(kw));
        }

        int totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(v => v.AvailableQuantity - v.MinimumStockQuantity)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (totalCount, items);
    }

    public async Task<(int TotalCount, List<VwExpiringLotAlert> Items)> GetExpiringLotAlertsAsync(
        string? keyword, int? maxDaysToExpiry, int pageNumber, int pageSize)
    {
        var query = _context.Set<VwExpiringLotAlert>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(v => v.ProductCode.ToLower().Contains(kw) || v.ProductName.ToLower().Contains(kw));
        }

        if (maxDaysToExpiry.HasValue)
        {
            query = query.Where(v => v.DaysToExpiry != null && v.DaysToExpiry <= maxDaysToExpiry.Value);
        }

        int totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(v => v.DaysToExpiry)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (totalCount, items);
    }

}
