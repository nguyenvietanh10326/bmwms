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

    public async Task<(List<VwInboundReport> Items, decimal TotalExpected, decimal TotalReceived, decimal TotalDamaged, decimal TotalShortage)> 
        GetInboundReportAsync(DateTime? fromDate, DateTime? toDate, string? productSearch, string? status)
    {
        var query = _context.VwInboundReports.AsQueryable();

        if (fromDate.HasValue)
        {
            var fDate = DateOnly.FromDateTime(fromDate.Value);
            query = query.Where(q => q.ExpectedReceiptDate >= fDate);
        }

        if (toDate.HasValue)
        {
            var tDate = DateOnly.FromDateTime(toDate.Value);
            query = query.Where(q => q.ExpectedReceiptDate <= tDate);
        }

        if (!string.IsNullOrEmpty(productSearch))
        {
            var search = productSearch.ToLower();
            query = query.Where(q => q.ProductCode.ToLower().Contains(search) || q.ProductName.ToLower().Contains(search));
        }
        
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(q => q.Status == status);
        }

        var totalExpected = await query.SumAsync(q => q.ExpectedQuantity);
        var totalReceived = await query.SumAsync(q => q.ReceivedQuantity);
        var totalDamaged = await query.SumAsync(q => q.DamagedQuantity);
        var totalShortage = await query.SumAsync(q => q.ShortageQuantity);

        var items = await query
            .OrderByDescending(q => q.ExpectedReceiptDate)
            .ThenBy(q => q.InboundOrderNumber)
            .ToListAsync();

        return (items, totalExpected, totalReceived, totalDamaged, totalShortage);
    }
}
