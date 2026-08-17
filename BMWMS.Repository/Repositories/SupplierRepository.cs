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
            .Include(s => s.SupplierProducts)
                .ThenInclude(sp => sp.Product)
                    .ThenInclude(p => p.UnitOfMeasure)
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

    public async Task<bool> HasPurchaseReferencesAsync(long supplierId)
    {
        return await _context.PurchaseOrders.AnyAsync(po => po.SupplierId == supplierId);
    }

    public async Task UpdateAsync(Supplier supplier)
    {
        _context.Suppliers.Update(supplier);
        await _context.SaveChangesAsync();
    }

    public async Task<(IEnumerable<SupplierProduct> Items, int TotalCount)> GetSupplierProductsAsync(long supplierId, string? search, string? status, int page, int pageSize)
    {
        var query = _context.SupplierProducts
            .Include(sp => sp.Product)
                .ThenInclude(p => p.UnitOfMeasure)
            .Where(sp => sp.SupplierId == supplierId);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(sp => sp.Status == status);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(sp => sp.Product.ProductCode.Contains(search) 
                                   || sp.Product.ProductName.Contains(search) 
                                   || (sp.SupplierProductCode != null && sp.SupplierProductCode.Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(sp => sp.Product.ProductName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(IEnumerable<InboundOrder> Items, int TotalCount)> GetSupplierInboundHistoryAsync(long supplierId, string? keyword, string? status, long? warehouseId, DateTime? fromDate, DateTime? toDate, int pageIndex, int pageSize)
    {
        var query = _context.InboundOrders
            .Include(x => x.PurchaseOrder)
                .ThenInclude(po => po.Supplier)
            .Include(x => x.Warehouse)
            .Include(x => x.InboundOrderItems)
            .Where(x => x.PurchaseOrder != null && x.PurchaseOrder.SupplierId == supplierId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(keyword))
        {
            query = query.Where(x => x.InboundOrderNumber.Contains(keyword) || 
                                     (x.PurchaseOrder != null && x.PurchaseOrder.PurchaseOrderNumber.Contains(keyword)));
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (warehouseId.HasValue)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }

        if (fromDate.HasValue)
        {
            var from = fromDate.Value.Date;
            query = query.Where(x => x.CreatedAt >= from);
        }

        if (toDate.HasValue)
        {
            var to = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.CreatedAt <= to);
        }

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<InboundOrder?> GetSupplierInboundHistoryDetailAsync(long supplierId, string inboundOrderNumber)
    {
        return await _context.InboundOrders
            .Include(x => x.PurchaseOrder)
                .ThenInclude(po => po.Supplier)
            .Include(x => x.Warehouse)
            .Include(x => x.InboundOrderItems)
                .ThenInclude(d => d.Product)
                    .ThenInclude(p => p.UnitOfMeasure)
            .FirstOrDefaultAsync(x => x.InboundOrderNumber == inboundOrderNumber && x.PurchaseOrder != null && x.PurchaseOrder.SupplierId == supplierId);
    }

    public async Task AssignProductsAsync(long supplierId, List<long> productIds)
    {
        // Delete all old mappings
        var existingMappings = await _context.SupplierProducts.Where(x => x.SupplierId == supplierId).ToListAsync();
        _context.SupplierProducts.RemoveRange(existingMappings);

        // Add new mappings
        foreach (var productId in productIds)
        {
            _context.SupplierProducts.Add(new SupplierProduct
            {
                SupplierId = supplierId,
                ProductId = productId,
                Status = "ACTIVE",
                IsPreferred = false
            });
        }

        await _context.SaveChangesAsync();
    }
}
