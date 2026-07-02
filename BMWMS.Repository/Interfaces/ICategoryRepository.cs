using BMWMS.Repository.Entities;

namespace BMWMS.Repository.Interfaces
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync();
    }
}
