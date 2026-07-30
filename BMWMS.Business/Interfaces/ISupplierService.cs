using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Supplier;

namespace BMWMS.Business.Interfaces;

public interface ISupplierService
{
    Task<PagedResultDto<SupplierListResponseDto>> GetPagedListAsync(SupplierFilterDto filter);
    Task<SupplierDetailResponseDto?> GetSupplierDetailAsync(string supplierCode);
}
