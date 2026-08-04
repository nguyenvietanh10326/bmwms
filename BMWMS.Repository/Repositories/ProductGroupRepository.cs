using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BMWMS.Repository.Repositories
{
    public class ProductGroupRepository : IProductGroupRepository
    {
        private readonly BmwmsContext _context;

        public ProductGroupRepository(BmwmsContext context)
        {
            _context = context;
        }

        public async Task<(List<ProductGroup> Items, int TotalCount)> GetPagedListAsync(
            string? keyword,
            string? status,
            int pageIndex,
            int pageSize)
        {
            var query = _context.ProductGroups
                .Include(g => g.Products)
                .Include(g => g.ProductGroupAttributes)
                    .ThenInclude(pga => pga.ProductAttribute)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim().ToLower();
                query = query.Where(g => g.GroupCode.ToLower().Contains(kw) || g.GroupName.ToLower().Contains(kw));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(g => g.Status == status);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(g => g.GroupCode)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<ProductGroup>> GetAllActiveAsync()
        {
            return await _context.ProductGroups
                .Where(g => g.Status == "ACTIVE")
                .Include(g => g.Products)
                .Include(g => g.ProductGroupAttributes)
                .OrderBy(g => g.GroupCode)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<ProductGroup?> GetByIdAsync(long productGroupId, bool includeAttributes = false)
        {
            var query = _context.ProductGroups
                .Include(g => g.Products)
                .AsQueryable();

            if (includeAttributes)
            {
                query = query
                    .Include(g => g.ProductGroupAttributes.OrderBy(a => a.DisplayOrder))
                        .ThenInclude(pga => pga.ProductAttribute)
                            .ThenInclude(pa => pa.ProductAttributeOptions.Where(o => o.IsActive).OrderBy(o => o.DisplayOrder));
            }

            return await query.AsNoTracking().FirstOrDefaultAsync(g => g.ProductGroupId == productGroupId);
        }

        public async Task<ProductGroup?> GetByCodeAsync(string groupCode)
        {
            return await _context.ProductGroups
                .Include(g => g.ProductGroupAttributes)
                    .ThenInclude(pga => pga.ProductAttribute)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.GroupCode == groupCode);
        }

        public async Task<List<ProductGroupAttribute>> GetAttributesByGroupIdAsync(long productGroupId)
        {
            return await _context.ProductGroupAttributes
                .Where(pga => pga.ProductGroupId == productGroupId)
                .Include(pga => pga.ProductAttribute)
                    .ThenInclude(pa => pa.ProductAttributeOptions.Where(o => o.IsActive).OrderBy(o => o.DisplayOrder))
                .OrderBy(pga => pga.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<ProductAttribute>> GetAllAttributesAsync()
        {
            return await _context.ProductAttributes
                .Include(pa => pa.ProductAttributeOptions.Where(o => o.IsActive).OrderBy(o => o.DisplayOrder))
                .Where(pa => pa.Status == "ACTIVE")
                .OrderBy(pa => pa.AttributeName)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<bool> IsGroupCodeExistsAsync(string groupCode, long? excludeId = null)
        {
            return await _context.ProductGroups.AnyAsync(g =>
                g.GroupCode == groupCode &&
                (!excludeId.HasValue || g.ProductGroupId != excludeId.Value));
        }

        public async Task<long> AddAsync(ProductGroup group)
        {
            await _context.ProductGroups.AddAsync(group);
            await _context.SaveChangesAsync();
            return group.ProductGroupId;
        }

        public async Task UpdateAsync(ProductGroup group)
        {
            _context.ProductGroups.Update(group);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(long productGroupId)
        {
            var group = await _context.ProductGroups.FindAsync(productGroupId);
            if (group != null)
            {
                // Remove group attributes first
                var attrs = await _context.ProductGroupAttributes.Where(pga => pga.ProductGroupId == productGroupId).ToListAsync();
                _context.ProductGroupAttributes.RemoveRange(attrs);

                _context.ProductGroups.Remove(group);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasProductsAsync(long productGroupId)
        {
            return await _context.Products.AnyAsync(p => p.ProductGroupId == productGroupId);
        }

        public async Task UpdateGroupAttributesAsync(long productGroupId, List<ProductGroupAttribute> attributes)
        {
            var existing = await _context.ProductGroupAttributes
                .Where(pga => pga.ProductGroupId == productGroupId)
                .ToListAsync();

            _context.ProductGroupAttributes.RemoveRange(existing);

            foreach (var attr in attributes)
            {
                attr.ProductGroupId = productGroupId;
                await _context.ProductGroupAttributes.AddAsync(attr);
            }

            await _context.SaveChangesAsync();
        }
    }
}
