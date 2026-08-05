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

    public async Task<(List<VwInventoryAvailability> Items, int TotalCount, decimal TotalOnHand, decimal TotalReserved, decimal TotalAvailable)> 
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
        
        // Sums need to be calculated on the filtered query
        decimal totalOnHand = await query.SumAsync(v => v.OnHandQuantity);
        decimal totalReserved = await query.SumAsync(v => v.ReservedQuantity);
        decimal totalAvailable = await query.SumAsync(v => v.AvailableQuantity ?? 0);

        var items = await query
            .OrderBy(v => v.ProductCode)
            .ThenBy(v => v.LotNumber)
            .ThenBy(v => v.LocationCode)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount, totalOnHand, totalReserved, totalAvailable);
    }

    public async Task<(int TotalCount, decimal TotalExpected, decimal TotalReceived, decimal TotalDamaged, decimal TotalShortage, List<VwInboundReport> Items)> GetInboundReportAsync(
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
        var totalExpected = await query.SumAsync(x => (decimal?)x.ExpectedQuantity) ?? 0;
        var totalReceived = await query.SumAsync(x => (decimal?)x.ReceivedQuantity) ?? 0;
        var totalDamaged = await query.SumAsync(x => (decimal?)x.DamagedQuantity) ?? 0;
        var totalShortage = await query.SumAsync(x => (decimal?)x.ShortageQuantity) ?? 0;

        var items = await query
            .OrderByDescending(x => x.ExpectedReceiptDate)
            .ThenBy(x => x.InboundOrderNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (totalCount, totalExpected, totalReceived, totalDamaged, totalShortage, items);
    }

    public async Task<(int TotalCount, decimal TotalRequested, decimal TotalIssued, List<VwOutboundReport> Items)> GetOutboundReportAsync(
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
        var totalRequested = await query.SumAsync(x => (decimal?)x.RequestedQuantity) ?? 0;
        var totalIssued = await query.SumAsync(x => (decimal?)x.IssuedQuantity) ?? 0;

        var items = await query
            .OrderByDescending(x => x.ExpectedIssueDate)
            .ThenBy(x => x.OutboundOrderNumber)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (totalCount, totalRequested, totalIssued, items);
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

    public async Task<(int TotalCount, List<(long ProductId, string ProductCode, string ProductName, string BaseUnitCode, decimal CurrentStock, decimal InboundQuantity, decimal OutboundQuantity, decimal AdjustmentQuantity, int MovementFrequency, int DaysSinceLastMovement)> Items)> GetProductStatisticsAsync(
        DateTime? fromDate, DateTime? toDate, string? productSearch, string? productGroupCode, int pageNumber, int pageSize)
    {
        var productQuery = _context.Products
            .Include(p => p.UnitOfMeasure)
            .Include(p => p.ProductGroup)
            .AsQueryable();

        if (!string.IsNullOrEmpty(productSearch))
        {
            var search = productSearch.ToLower();
            productQuery = productQuery.Where(p => p.ProductCode.ToLower().Contains(search) || p.ProductName.ToLower().Contains(search));
        }

        if (!string.IsNullOrEmpty(productGroupCode))
        {
            productQuery = productQuery.Where(p => p.ProductGroup.GroupCode == productGroupCode);
        }

        var totalCount = await productQuery.CountAsync();

        var products = await productQuery
            .OrderBy(p => p.ProductCode)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new {
                p.ProductId,
                p.ProductCode,
                p.ProductName,
                UnitCode = p.UnitOfMeasure.UnitCode,
                CurrentStock = p.Inventories.Sum(i => i.OnHandQuantity)
            })
            .ToListAsync();

        var productIds = products.Select(p => p.ProductId).ToList();

        var transQuery = _context.InventoryTransactions.Where(t => productIds.Contains(t.ProductId));
        if (fromDate.HasValue) transQuery = transQuery.Where(t => t.TransactionAt >= fromDate.Value);
        if (toDate.HasValue) transQuery = transQuery.Where(t => t.TransactionAt <= toDate.Value);

        var transactions = await transQuery
            .GroupBy(t => t.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                InboundQuantity = g.Where(t => t.TransactionType == "Inbound").Sum(t => t.OnHandDelta),
                OutboundQuantity = g.Where(t => t.TransactionType == "Outbound").Sum(t => Math.Abs(t.OnHandDelta)),
                AdjustmentQuantity = g.Where(t => t.TransactionType == "Adjustment").Sum(t => t.OnHandDelta),
                MovementFrequency = g.Count(),
                LastMovement = g.Max(t => (DateTime?)t.TransactionAt)
            })
            .ToDictionaryAsync(x => x.ProductId);

        var items = new List<(long, string, string, string, decimal, decimal, decimal, decimal, int, int)>();
        var now = DateTime.UtcNow;

        foreach (var p in products)
        {
            var stat = transactions.GetValueOrDefault(p.ProductId);
            int daysSinceLast = stat?.LastMovement != null ? (now - stat.LastMovement.Value).Days : 0;

            items.Add((
                p.ProductId,
                p.ProductCode,
                p.ProductName,
                p.UnitCode,
                p.CurrentStock,
                stat?.InboundQuantity ?? 0,
                stat?.OutboundQuantity ?? 0,
                stat?.AdjustmentQuantity ?? 0,
                stat?.MovementFrequency ?? 0,
                daysSinceLast
            ));
        }

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

    public async Task<(int TotalCount, List<(long StocktakeSessionId, string StocktakeNumber, DateOnly PlannedDate, string Status, int BinsCounted, int MatchedItems, int ShortageItems, int ExcessItems, int TotalItemsCounted, decimal TotalShortageQuantity, decimal TotalExcessQuantity, decimal TotalApprovedAdjustmentQuantity)> Items)> GetStocktakeStatisticsAsync(
        DateTime? fromDate, DateTime? toDate, string? countType, string? storageAreaCode, string? productGroupCode, string? sessionStatus, int pageNumber, int pageSize)
    {
        var query = _context.StocktakeSessions.Include(s => s.StocktakeItems).AsQueryable();

        if (fromDate.HasValue)
        {
            var fDate = DateOnly.FromDateTime(fromDate.Value);
            query = query.Where(x => x.PlannedDate >= fDate);
        }
        if (toDate.HasValue)
        {
            var tDate = DateOnly.FromDateTime(toDate.Value);
            query = query.Where(x => x.PlannedDate <= tDate);
        }
        if (!string.IsNullOrEmpty(sessionStatus))
        {
            var statusUpper = sessionStatus.ToUpper();
            query = query.Where(x => x.Status == statusUpper);
        }

        var totalCount = await query.CountAsync();

        var sessions = await query
            .OrderByDescending(x => x.PlannedDate)
            .ThenByDescending(x => x.StocktakeSessionId)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new {
                s.StocktakeSessionId,
                s.StocktakeNumber,
                s.PlannedDate,
                s.Status,
                BinsCounted = s.StocktakeItems.Select(i => i.StorageLocationId).Distinct().Count(),
                MatchedItems = s.StocktakeItems.Count(i => i.DifferenceQuantity == 0),
                ShortageItems = s.StocktakeItems.Count(i => i.DifferenceQuantity < 0),
                ExcessItems = s.StocktakeItems.Count(i => i.DifferenceQuantity > 0),
                TotalItemsCounted = s.StocktakeItems.Count(),
                TotalShortageQuantity = s.StocktakeItems.Where(i => i.DifferenceQuantity < 0).Sum(i => i.DifferenceQuantity) ?? 0,
                TotalExcessQuantity = s.StocktakeItems.Where(i => i.DifferenceQuantity > 0).Sum(i => i.DifferenceQuantity) ?? 0,
                TotalApprovedAdjustmentQuantity = s.StocktakeItems.Sum(i => i.AdjustmentQuantity) ?? 0
            })
            .ToListAsync();

        var items = sessions.Select(s => (
            s.StocktakeSessionId,
            s.StocktakeNumber,
            s.PlannedDate,
            s.Status,
            s.BinsCounted,
            s.MatchedItems,
            s.ShortageItems,
            s.ExcessItems,
            s.TotalItemsCounted,
            s.TotalShortageQuantity,
            s.TotalExcessQuantity,
            s.TotalApprovedAdjustmentQuantity
        )).ToList();

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

    public async Task<(int TotalCount, List<VwOverdueOrder> Items)> GetOverdueOrderAlertsAsync(
        string? documentType, string? keyword, int pageNumber, int pageSize)
    {
        var query = _context.Set<VwOverdueOrder>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(documentType))
        {
            query = query.Where(v => v.DocumentType == documentType);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(v => v.DocumentNumber.ToLower().Contains(kw));
        }

        int totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(v => v.DaysOverdue)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (totalCount, items);
    }

    public async Task<List<VwWarehouseKpi>> GetWarehouseKpisAsync()
    {
        return await _context.VwWarehouseKpis.AsNoTracking().ToListAsync();
    }
}
