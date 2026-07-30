using BMWMS.Repository.Models;
using BMWMS.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BMWMS.Repository.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly BmwmsContext _context;

    public SupplierRepository(BmwmsContext context)
    {
        _context = context;
    }

    public async Task<(List<Supplier> Items, int TotalCount)> GetPagedListAsync(string? keyword, string? status, int pageIndex, int pageSize)
    {
        var query = _context.Suppliers
            .Include(s => s.SupplierProducts)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.ToLower();
            query = query.Where(s =>
                s.SupplierCode.ToLower().Contains(kw) ||
                s.SupplierName.ToLower().Contains(kw) ||
                s.PhoneNumber.ToLower().Contains(kw) ||
                s.RepresentativeName.ToLower().Contains(kw) ||
                s.TaxCode.ToLower().Contains(kw)
            );
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(s => s.Status == status);
        }

        var totalCount = await query.CountAsync();

        // BR-02: Results are sorted by supplier name ascending
        var items = await query
            .OrderBy(s => s.SupplierName)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Supplier?> GetSupplierByCodeAsync(string supplierCode)
    {
        return await _context.Suppliers
            .Include(s => s.SupplierProducts)
                .ThenInclude(sp => sp.Product)
                    .ThenInclude(p => p.ProductGroup)
            .FirstOrDefaultAsync(s => s.SupplierCode == supplierCode);
    }

    public async Task<decimal> GetSupplierYtdInboundValueAsync(long supplierId, int year)
    {
        return await _context.PurchaseOrderDetails
            .Include(pod => pod.PurchaseOrder)
            .Where(pod => pod.PurchaseOrder.SupplierId == supplierId && pod.PurchaseOrder.OrderDate.Year == year)
            .SumAsync(pod => pod.OrderedQuantity * (pod.UnitPrice ?? 0));
    }

    public async Task<bool> CheckSupplierCodeExistsAsync(string code)
    {
        return await _context.Suppliers.AnyAsync(s => s.SupplierCode == code);
    }

    public async Task<bool> CheckTaxCodeExistsAsync(string taxCode)
    {
        return await _context.Suppliers.AnyAsync(s => s.TaxCode == taxCode);
    }

    public async Task AddAsync(Supplier supplier)
    {
        await _context.Suppliers.AddAsync(supplier);
        await _context.SaveChangesAsync();
    }

    public async Task AddAuditLogAsync(AuditLog log)
    {
        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }
}
