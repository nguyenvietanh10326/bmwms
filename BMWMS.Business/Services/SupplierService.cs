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

    public async Task<SupplierDetailResponseDto?> GetSupplierDetailAsync(string supplierCode)
    {
        var supplier = await _supplierRepository.GetSupplierByCodeAsync(supplierCode);
        if (supplier == null)
            return null;

        var ytdValue = await _supplierRepository.GetSupplierYtdInboundValueAsync(supplier.SupplierId, DateTime.Now.Year);

        return new SupplierDetailResponseDto
        {
            SupplierId = supplier.SupplierId,
            SupplierCode = supplier.SupplierCode,
            SupplierName = supplier.SupplierName,
            TaxCode = supplier.TaxCode,
            PhoneNumber = supplier.PhoneNumber,
            Email = supplier.Email,
            Address = supplier.Address,
            RepresentativeName = supplier.RepresentativeName,
            Status = supplier.Status,
            InboundYtdValue = ytdValue,
            TotalProducts = supplier.SupplierProducts.Count,
            PreferredProducts = supplier.SupplierProducts.Count(sp => sp.IsPreferred),
            SuppliedProducts = supplier.SupplierProducts.Select(sp => new SupplierProductDto
            {
                ProductCode = sp.Product.ProductCode,
                ProductName = sp.Product.ProductName,
                GroupName = sp.Product.ProductGroup.GroupName,
                LastPurchasePrice = sp.LastPurchasePrice,
                LeadTimeDays = sp.LeadTimeDays,
                Status = sp.Status
            }).ToList()
        };
    }

    public async Task<long> CreateSupplierAsync(SupplierCreateRequestDto dto, long creatorId, string? ipAddress)
    {
        // Validation
        if (await _supplierRepository.CheckSupplierCodeExistsAsync(dto.SupplierCode))
        {
            throw new ArgumentException("Mã nhà cung cấp đã tồn tại.");
        }

        if (await _supplierRepository.CheckTaxCodeExistsAsync(dto.TaxCode))
        {
            throw new ArgumentException("Mã số thuế đã tồn tại.");
        }

        var supplier = new Repository.Models.Supplier
        {
            SupplierCode = dto.SupplierCode,
            SupplierName = dto.SupplierName,
            TaxCode = dto.TaxCode,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            RepresentativeName = dto.RepresentativeName,
            Status = "ACTIVE",
            CreatedByUserId = creatorId,
            CreatedAt = DateTime.UtcNow
        };

        await _supplierRepository.AddAsync(supplier);

        // Audit Log
        var newValues = $"{{ \"SupplierCode\": \"{supplier.SupplierCode}\", \"SupplierName\": \"{supplier.SupplierName}\", \"TaxCode\": \"{supplier.TaxCode}\", \"Status\": \"{supplier.Status}\" }}";
        await _supplierRepository.AddAuditLogAsync(new Repository.Models.AuditLog
        {
            UserId = creatorId,
            ActionType = "CREATE_SUPPLIER",
            EntityName = "Supplier",
            EntityId = supplier.SupplierId.ToString(),
            NewValuesJson = newValues,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });

        return supplier.SupplierId;
    }
}
