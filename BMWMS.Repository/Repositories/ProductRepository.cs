using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BMWMS.Repository.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly BmwmsContext _context;

        public ProductRepository(BmwmsContext context)
        {
            _context = context;
        }

        public async Task<Product?> GetByIdAsync(long productId, bool includeDetails = false)
        {
            var query = _context.Products
                .Include(p => p.ProductGroup)
                .Include(p => p.UnitOfMeasure)
                .Include(p => p.CreatedByUser)
                .Include(p => p.UpdatedByUser)
                .Include(p => p.ProductAttributeValues)
                    .ThenInclude(pav => pav.ProductAttribute)
                        .ThenInclude(pa => pa.ProductAttributeOptions.Where(o => o.IsActive))
                .AsQueryable();

            if (includeDetails)
            {
                query = query
                    .Include(p => p.ProductWarehousePolicies)
                        .ThenInclude(pwp => pwp.Warehouse)
                    .Include(p => p.SupplierProducts)
                        .ThenInclude(sp => sp.Supplier)
                    .Include(p => p.Inventories)
                        .ThenInclude(inv => inv.StorageLocation)
                            .ThenInclude(sl => sl.Warehouse);
            }

            return await query.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task<Product?> GetByCodeAsync(string productCode)
        {
            return await _context.Products
                .Include(p => p.ProductGroup)
                .Include(p => p.UnitOfMeasure)
                .Include(p => p.ProductAttributeValues)
                    .ThenInclude(pav => pav.ProductAttribute)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductCode == productCode);
        }

        public async Task<(List<Product> Items, int TotalCount)> GetPagedListAsync(
            string? keyword,
            long? productGroupId,
            int? unitOfMeasureId,
            string? status,
            string? rotationMethod,
            int pageIndex,
            int pageSize)
        {
            var query = _context.Products
                .Include(p => p.ProductGroup)
                .Include(p => p.UnitOfMeasure)
                .Include(p => p.Inventories)
                .AsNoTracking()
                .AsQueryable();

            // 1. Keyword search (Code, Name, Barcode)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(p => p.ProductCode.ToLower().Contains(kw)
                                      || p.ProductName.ToLower().Contains(kw)
                                      || (p.Barcode != null && p.Barcode.ToLower().Contains(kw)));
            }

            // 2. Filter by Product Group / Category
            if (productGroupId.HasValue && productGroupId.Value > 0)
            {
                query = query.Where(p => p.ProductGroupId == productGroupId.Value);
            }

            // 3. Filter by Unit of Measure
            if (unitOfMeasureId.HasValue && unitOfMeasureId.Value > 0)
            {
                query = query.Where(p => p.UnitOfMeasureId == unitOfMeasureId.Value);
            }

            // 4. Filter by Status (ACTIVE / INACTIVE)
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(p => p.Status == status);
            }

            // 5. Filter by Rotation Method (FIFO / FEFO / LIFO)
            if (!string.IsNullOrWhiteSpace(rotationMethod))
            {
                query = query.Where(p => p.RotationMethod == rotationMethod);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.ProductCode)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<UnitsOfMeasure>> GetUnitsOfMeasureAsync()
        {
            return await _context.UnitsOfMeasures
                .Where(u => u.Status == "ACTIVE")
                .OrderBy(u => u.UnitName)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<ProductGroup>> GetProductGroupsAsync()
        {
            return await _context.ProductGroups
                .Where(g => g.Status == "ACTIVE")
                .OrderBy(g => g.GroupCode)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> IsCodeExistsAsync(string productCode, long? excludeProductId = null)
        {
            return await _context.Products.AnyAsync(p =>
                p.ProductCode == productCode &&
                (!excludeProductId.HasValue || p.ProductId != excludeProductId.Value));
        }

        public async Task<bool> IsBarcodeExistsAsync(string barcode, long? excludeProductId = null)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return false;

            return await _context.Products.AnyAsync(p =>
                p.Barcode == barcode &&
                (!excludeProductId.HasValue || p.ProductId != excludeProductId.Value));
        }

        public async Task<bool> HasTransactionsOrInventoryAsync(long productId)
        {
            var hasInventory = await _context.Inventories.AnyAsync(i => i.ProductId == productId && i.OnHandQuantity > 0);
            if (hasInventory) return true;

            var hasTransactions = await _context.InventoryTransactions.AnyAsync(t => t.ProductId == productId);
            if (hasTransactions) return true;

            var hasInbound = await _context.InboundOrderItems.AnyAsync(item => item.ProductId == productId);
            if (hasInbound) return true;

            var hasOutbound = await _context.OutboundOrderItems.AnyAsync(item => item.ProductId == productId);
            if (hasOutbound) return true;

            return false;
        }

        public async Task<long> AddAsync(Product product, List<ProductAttributeValue>? attributeValues = null)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            if (attributeValues != null && attributeValues.Any())
            {
                foreach (var pav in attributeValues)
                {
                    pav.ProductId = product.ProductId;
                    pav.UpdatedAt = DateTime.UtcNow;
                    await _context.ProductAttributeValues.AddAsync(pav);
                }
                await _context.SaveChangesAsync();
            }

            return product.ProductId;
        }

        public async Task UpdateAsync(Product product, List<ProductAttributeValue>? attributeValues = null)
        {
            _context.Products.Update(product);

            if (attributeValues != null)
            {
                var existingValues = await _context.ProductAttributeValues
                    .Where(pav => pav.ProductId == product.ProductId)
                    .ToListAsync();

                _context.ProductAttributeValues.RemoveRange(existingValues);

                foreach (var pav in attributeValues)
                {
                    pav.ProductId = product.ProductId;
                    pav.UpdatedAt = DateTime.UtcNow;
                    await _context.ProductAttributeValues.AddAsync(pav);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                // Remove attribute values first
                var attrVals = await _context.ProductAttributeValues.Where(v => v.ProductId == productId).ToListAsync();
                _context.ProductAttributeValues.RemoveRange(attrVals);

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<ProductAttributeValue>> GetProductAttributeValuesAsync(long productId)
        {
            return await _context.ProductAttributeValues
                .Where(v => v.ProductId == productId)
                .Include(v => v.ProductAttribute)
                    .ThenInclude(pa => pa.ProductAttributeOptions.Where(o => o.IsActive))
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
