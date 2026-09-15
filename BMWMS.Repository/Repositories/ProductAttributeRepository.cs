using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace BMWMS.Repository.Repositories;

public class ProductAttributeRepository : IProductAttributeRepository
{
    private readonly BmwmsContext _context;

    public ProductAttributeRepository(BmwmsContext context)
    {
        _context = context;
    }

    public async Task<(List<ProductAttribute> Items, int TotalCount)> GetPagedAsync(string? keyword, string? status, int pageIndex, int pageSize)
    {
        var query = _context.ProductAttributes.Include(a => a.ProductAttributeOptions).AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            keyword = keyword.ToLower();
            query = query.Where(a => a.AttributeCode.ToLower().Contains(keyword) || a.AttributeName.ToLower().Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status);
        }

        int totalCount = await query.CountAsync();
        
        var items = await query
            .OrderBy(a => a.AttributeName)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<ProductAttribute>> GetAllAsync()
    {
        return await _context.ProductAttributes
            .Include(a => a.ProductAttributeOptions)
            .OrderBy(a => a.AttributeName)
            .ToListAsync();
    }

    public async Task<ProductAttribute?> GetByIdAsync(long id)
    {
        return await _context.ProductAttributes
            .Include(a => a.ProductAttributeOptions)
            .FirstOrDefaultAsync(a => a.ProductAttributeId == id);
    }

    public async Task<ProductAttribute> AddAsync(ProductAttribute attribute)
    {
        _context.ProductAttributes.Add(attribute);
        await _context.SaveChangesAsync();
        return attribute;
    }

    public async Task UpdateAsync(ProductAttribute attribute)
    {
        var existing = await _context.ProductAttributes
            .Include(a => a.ProductAttributeOptions)
            .FirstOrDefaultAsync(a => a.ProductAttributeId == attribute.ProductAttributeId);

        if (existing == null) throw new KeyNotFoundException("Thuộc tính không tồn tại.");

        existing.AttributeCode = attribute.AttributeCode;
        existing.AttributeName = attribute.AttributeName;
        existing.DataType = attribute.DataType;
        existing.UnitLabel = attribute.UnitLabel;
        existing.Description = attribute.Description;
        existing.Status = attribute.Status;

        // Update existing and add new
        if (attribute.ProductAttributeOptions != null && attribute.ProductAttributeOptions.Any())
        {
            foreach (var newOption in attribute.ProductAttributeOptions)
            {
                var existingOption = existing.ProductAttributeOptions
                    .FirstOrDefault(o => o.ProductAttributeOptionId != 0 && o.ProductAttributeOptionId == newOption.ProductAttributeOptionId);

                if (existingOption != null)
                {
                    existingOption.OptionCode = newOption.OptionCode;
                    existingOption.OptionValue = newOption.OptionValue;
                    existingOption.DisplayOrder = newOption.DisplayOrder;
                    existingOption.IsActive = newOption.IsActive;
                }
                else
                {
                    existing.ProductAttributeOptions.Add(new ProductAttributeOption
                    {
                        OptionCode = newOption.OptionCode,
                        OptionValue = newOption.OptionValue,
                        DisplayOrder = newOption.DisplayOrder,
                        IsActive = newOption.IsActive
                    });
                }
            }
        }

        // Inactivate missing
        var newOptionIds = attribute.ProductAttributeOptions?.Select(o => o.ProductAttributeOptionId).ToList() ?? new List<long>();
        foreach (var oldOption in existing.ProductAttributeOptions)
        {
            if (oldOption.ProductAttributeOptionId != 0 && !newOptionIds.Contains(oldOption.ProductAttributeOptionId))
            {
                oldOption.IsActive = false; // Soft delete
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var attribute = await _context.ProductAttributes.FindAsync(id);
        if (attribute == null) return false;

        _context.ProductAttributes.Remove(attribute);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsAttributeUsedAsync(long id)
    {
        bool usedInGroup = await _context.ProductGroupAttributes.AnyAsync(pga => pga.ProductAttributeId == id);
        bool usedInProduct = await _context.ProductAttributeValues.AnyAsync(pav => pav.ProductAttributeId == id);
        return usedInGroup || usedInProduct;
    }
}
