using BMWMS.Business.Common;
using BMWMS.Business.DTOs.ProductGroup;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.Business.Interfaces
{
    public interface IProductGroupService
    {
        Task<PagedResultDto<ProductGroupResponseDto>> GetPagedListAsync(ProductGroupFilterDto filter);
        Task<List<ProductGroupResponseDto>> GetAllActiveAsync();
        Task<ProductGroupDetailDto?> GetByIdAsync(long productGroupId);
        Task<List<GroupAttributeConfigDto>> GetAttributesByGroupIdAsync(long productGroupId);
        Task<List<ProductAttributeDto>> GetAllAttributesAsync();
        Task<long> CreateAsync(CreateProductGroupDto dto, long userId);
        Task UpdateAsync(long productGroupId, UpdateProductGroupDto dto, long userId);
        Task ToggleStatusAsync(long productGroupId, long userId);
        Task DeleteAsync(long productGroupId, long userId);
        Task UpdateGroupAttributesAsync(long productGroupId, List<GroupAttributeAssignmentDto> attributes, long userId);
    }
}
