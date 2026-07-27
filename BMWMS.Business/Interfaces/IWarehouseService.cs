using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Warehouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Business.Interfaces
{
    public interface IWarehouseService
    {
        Task<WarehouseResponseDto?> GetByIdAsync(long id);
        Task<PagedResultDto<WarehouseResponseDto>> GetPagedListAsync(WarehouseFilterDto filter);
        Task<long> CreateAsync(CreateWarehouseDto dto);
        Task UpdateAsync(long id, UpdateWarehouseDto dto);
        Task DeleteAsync(long id);
    }
}
