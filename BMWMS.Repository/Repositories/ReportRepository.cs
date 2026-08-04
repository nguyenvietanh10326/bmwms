using BMWMS.Repository.Context;
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
}
