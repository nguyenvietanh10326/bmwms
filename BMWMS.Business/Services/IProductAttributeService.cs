using BMWMS.Business.DTOs.ProductAttribute;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.Business.Services;

public interface IProductAttributeService
{
    Task<BMWMS.Business.Common.PagedResultDto<ProductAttributeDto>> GetPagedAsync(string? keyword, string? status, int pageIndex, int pageSize);
    Task<List<ProductAttributeDto>> GetAllAsync();
    Task<ProductAttributeDto?> GetByIdAsync(long id);
    Task<ProductAttributeDto> CreateAsync(CreateProductAttributeDto dto);
    Task UpdateAsync(long id, UpdateProductAttributeDto dto);
    Task<bool> DeleteAsync(long id);
}
