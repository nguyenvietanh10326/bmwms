using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Product;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.Business.Interfaces
{
    public interface IProductService
    {
        Task<PagedResultDto<ProductResponseDto>> GetPagedListAsync(ProductFilterDto filter);
        Task<ProductDetailResponseDto?> GetByIdAsync(long productId);
        Task<List<UnitOfMeasureDto>> GetUnitsOfMeasureAsync();
        Task<List<ProductGroupOptionDto>> GetProductGroupsAsync();
        Task<long> CreateAsync(CreateProductDto dto, long currentUserId);
        Task UpdateAsync(long productId, UpdateProductDto dto, long currentUserId);
        Task ToggleStatusAsync(long productId, long currentUserId);
        Task DeleteAsync(long productId);
    }
}
