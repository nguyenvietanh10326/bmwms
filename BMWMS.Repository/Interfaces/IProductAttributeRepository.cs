using BMWMS.Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.Repository.Interfaces;

public interface IProductAttributeRepository
{
    Task<(List<ProductAttribute> Items, int TotalCount)> GetPagedAsync(string? keyword, string? status, int pageIndex, int pageSize);
    Task<List<ProductAttribute>> GetAllAsync();
    Task<ProductAttribute?> GetByIdAsync(long id);
    Task<ProductAttribute> AddAsync(ProductAttribute attribute);
    Task UpdateAsync(ProductAttribute attribute);
    Task<bool> DeleteAsync(long id);
    Task<bool> IsAttributeUsedAsync(long id);
}
