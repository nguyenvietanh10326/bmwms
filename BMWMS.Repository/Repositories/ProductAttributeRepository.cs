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

        // Delete old options from database
        _context.ProductAttributeOptions.RemoveRange(existing.ProductAttributeOptions);
        existing.ProductAttributeOptions.Clear();

        // Add new options
        if (attribute.ProductAttributeOptions != null && attribute.ProductAttributeOptions.Any())
        {
            foreach (var option in attribute.ProductAttributeOptions)
            {
                existing.ProductAttributeOptions.Add(new ProductAttributeOption
                {
                    OptionCode = option.OptionCode,
                    OptionValue = option.OptionValue,
                    DisplayOrder = option.DisplayOrder,
                    IsActive = option.IsActive
                });
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
