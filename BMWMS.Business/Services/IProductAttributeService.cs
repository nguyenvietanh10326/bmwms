using BMWMS.Business.DTOs.ProductAttribute;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.Business.Services;

public interface IProductAttributeService
{
    Task<List<ProductAttributeDto>> GetAllAsync();
    Task<ProductAttributeDto?> GetByIdAsync(long id);
    Task<ProductAttributeDto> CreateAsync(CreateProductAttributeDto dto);
    Task UpdateAsync(long id, UpdateProductAttributeDto dto);
    Task<bool> DeleteAsync(long id);
}
