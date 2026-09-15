using BMWMS.Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.Repository.Interfaces
{
    public interface IUnitOfMeasureRepository
    {
        Task<List<UnitsOfMeasure>> GetAllAsync(string? keyword);
        Task<UnitsOfMeasure?> GetByIdAsync(int id);
        Task<bool> ExistsByCodeAsync(string code, int? excludeId = null);
        Task AddAsync(UnitsOfMeasure entity);
        Task UpdateAsync(UnitsOfMeasure entity);
        Task DeleteAsync(UnitsOfMeasure entity);
        Task<bool> IsInUseAsync(int id);
    }
}
