using BMWMS.Business.DTOs;

namespace BMWMS.Business.Interfaces
{
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetAllAsync();
    }
}
