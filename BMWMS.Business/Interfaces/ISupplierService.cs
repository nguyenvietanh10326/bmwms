using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Supplier;

namespace BMWMS.Business.Interfaces;

public interface ISupplierService
{
    Task<PagedResultDto<SupplierListResponseDto>> GetPagedListAsync(SupplierFilterDto filter);
    Task<SupplierDetailResponseDto?> GetSupplierDetailAsync(string supplierCode);
    Task<long> CreateSupplierAsync(SupplierCreateRequestDto dto, long creatorId, string? ipAddress);
    Task UpdateSupplierAsync(string currentSupplierCode, SupplierUpdateRequestDto dto, long updaterId, string? ipAddress);
    Task<PagedResultDto<SupplierProductResponseDto>> GetSupplierProductsAsync(string supplierCode, SupplierProductFilterDto filter);
    Task<PagedResultDto<SupplierInboundHistoryResponseDto>> GetSupplierInboundHistoryAsync(string supplierCode, SupplierInboundHistoryFilterDto filter);
    Task<SupplierInboundHistoryDetailDto?> GetSupplierInboundHistoryDetailAsync(string supplierCode, string inboundOrderNumber);
}
