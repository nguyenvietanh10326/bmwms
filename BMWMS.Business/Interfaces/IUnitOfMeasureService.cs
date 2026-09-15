using BMWMS.Business.DTOs.Product;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BMWMS.Business.Interfaces
{
    public interface IUnitOfMeasureService
    {
        Task<List<UnitOfMeasureDto>> GetAllAsync(string? keyword = null);
        Task<UnitOfMeasureDto?> GetByIdAsync(int id);
        Task<int> CreateAsync(CreateUnitOfMeasureDto dto);
        Task UpdateAsync(int id, UpdateUnitOfMeasureDto dto);
        Task DeleteAsync(int id);
    }
}
