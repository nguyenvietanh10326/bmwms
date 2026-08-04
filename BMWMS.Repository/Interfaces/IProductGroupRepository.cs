using BMWMS.Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.Repository.Interfaces
{
    public interface IProductGroupRepository
    {
        Task<(List<ProductGroup> Items, int TotalCount)> GetPagedListAsync(string? keyword, string? status, int pageIndex, int pageSize);
        Task<List<ProductGroup>> GetAllActiveAsync();
        Task<ProductGroup?> GetByIdAsync(long productGroupId, bool includeAttributes = false);
        Task<ProductGroup?> GetByCodeAsync(string groupCode);
        Task<List<ProductGroupAttribute>> GetAttributesByGroupIdAsync(long productGroupId);
        Task<List<ProductAttribute>> GetAllAttributesAsync();
        Task<bool> IsGroupCodeExistsAsync(string groupCode, long? excludeId = null);
        Task<long> AddAsync(ProductGroup group);
        Task UpdateAsync(ProductGroup group);
        Task DeleteAsync(long productGroupId);
        Task<bool> HasProductsAsync(long productGroupId);
        Task UpdateGroupAttributesAsync(long productGroupId, List<ProductGroupAttribute> attributes);
    }
}
