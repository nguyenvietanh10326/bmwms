using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Supplier;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Interfaces;

namespace BMWMS.Business.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;

    public SupplierService(ISupplierRepository supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<PagedResultDto<SupplierListResponseDto>> GetPagedListAsync(SupplierFilterDto filter)
    {
        var (items, totalCount) = await _supplierRepository.GetPagedListAsync(
            filter.Keyword,
            filter.Status,
            filter.PageIndex,
            filter.PageSize);

        var dtos = items.Select(s => new SupplierListResponseDto
        {
            SupplierId = s.SupplierId,
            SupplierCode = s.SupplierCode,
            SupplierName = s.SupplierName,
            PhoneNumber = s.PhoneNumber,
            RepresentativeName = s.RepresentativeName,
            TaxCode = s.TaxCode,
            SuppliedProductCount = s.SupplierProducts.Count,
            UpdatedAt = s.UpdatedAt ?? s.CreatedAt,
            Status = s.Status
        }).ToList();

        return new PagedResultDto<SupplierListResponseDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize
        };
    }
}
