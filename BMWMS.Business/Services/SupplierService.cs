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

    public async Task UpdateSupplierAsync(string currentSupplierCode, SupplierUpdateRequestDto dto, long updaterId, string? ipAddress)
    {
        var supplier = await _supplierRepository.GetSupplierByCodeAsync(currentSupplierCode);
        if (supplier == null)
        {
            throw new ArgumentException("Nhà cung cấp không tồn tại.");
        }

        if (supplier.Status != "ACTIVE" && supplier.Status != "INACTIVE")
        {
            throw new InvalidOperationException("Không thể chỉnh sửa nhà cung cấp ở trạng thái hiện tại.");
        }

        if (supplier.SupplierCode != dto.SupplierCode)
        {
            if (await _supplierRepository.HasPurchaseReferencesAsync(supplier.SupplierId))
            {
                throw new InvalidOperationException("Không thể thay đổi Mã nhà cung cấp vì đã có dữ liệu đơn mua liên kết.");
            }
            if (await _supplierRepository.CheckSupplierCodeExistsAsync(dto.SupplierCode))
            {
                throw new ArgumentException("Mã nhà cung cấp mới đã tồn tại.");
            }
        }

        if (supplier.TaxCode != dto.TaxCode)
        {
            if (await _supplierRepository.CheckTaxCodeExistsAsync(dto.TaxCode))
            {
                throw new ArgumentException("Mã số thuế mới đã tồn tại.");
            }
        }

        var oldValues = $"{{ \"SupplierCode\": \"{supplier.SupplierCode}\", \"SupplierName\": \"{supplier.SupplierName}\", \"TaxCode\": \"{supplier.TaxCode}\", \"PhoneNumber\": \"{supplier.PhoneNumber}\", \"Email\": \"{supplier.Email}\", \"Address\": \"{supplier.Address}\", \"RepresentativeName\": \"{supplier.RepresentativeName}\", \"Status\": \"{supplier.Status}\" }}";

        supplier.SupplierCode = dto.SupplierCode;
        supplier.SupplierName = dto.SupplierName;
        supplier.TaxCode = dto.TaxCode;
        supplier.PhoneNumber = dto.PhoneNumber;
        supplier.Email = dto.Email;
        supplier.Address = dto.Address;
        supplier.RepresentativeName = dto.RepresentativeName;
        supplier.Status = dto.Status;
        supplier.UpdatedAt = DateTime.UtcNow;
        supplier.UpdatedByUserId = updaterId;

        await _supplierRepository.UpdateAsync(supplier);

        var newValues = $"{{ \"SupplierCode\": \"{supplier.SupplierCode}\", \"SupplierName\": \"{supplier.SupplierName}\", \"TaxCode\": \"{supplier.TaxCode}\", \"PhoneNumber\": \"{supplier.PhoneNumber}\", \"Email\": \"{supplier.Email}\", \"Address\": \"{supplier.Address}\", \"RepresentativeName\": \"{supplier.RepresentativeName}\", \"Status\": \"{supplier.Status}\" }}";

        await _supplierRepository.AddAuditLogAsync(new Repository.Models.AuditLog
        {
            UserId = updaterId,
            ActionType = "UPDATE_SUPPLIER",
            EntityName = "Supplier",
            EntityId = supplier.SupplierId.ToString(),
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task<PagedResultDto<SupplierProductResponseDto>> GetSupplierProductsAsync(string supplierCode, SupplierProductFilterDto filter)
    {
        var supplier = await _supplierRepository.GetSupplierByCodeAsync(supplierCode);
        if (supplier == null)
        {
            throw new ArgumentException($"Nhà cung cấp {supplierCode} không tồn tại.");
        }

        var (items, totalCount) = await _supplierRepository.GetSupplierProductsAsync(
            supplier.SupplierId, 
            filter.Search, 
            filter.Status, 
            filter.PageNumber, 
            filter.PageSize);

        var dtos = items.Select(sp => new SupplierProductResponseDto
        {
            ProductCode = sp.Product.ProductCode,
            ProductName = sp.Product.ProductName,
            UnitName = sp.Product.UnitOfMeasure.UnitName,
            SupplierProductCode = sp.SupplierProductCode,
            LeadTimeDays = sp.LeadTimeDays,
            Status = sp.Status
        }).ToList();

        return new PagedResultDto<SupplierProductResponseDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageIndex = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<PagedResultDto<SupplierInboundHistoryResponseDto>> GetSupplierInboundHistoryAsync(string supplierCode, SupplierInboundHistoryFilterDto filter)
    {
        var supplier = await _supplierRepository.GetSupplierByCodeAsync(supplierCode);
        if (supplier == null) throw new ArgumentException($"Nhà cung cấp {supplierCode} không tồn tại.");

        var (items, totalCount) = await _supplierRepository.GetSupplierInboundHistoryAsync(
            supplier.SupplierId, filter.Keyword, filter.Status, filter.WarehouseId, filter.FromDate, filter.ToDate, filter.PageIndex, filter.PageSize);

        var dtos = items.Select(io => new SupplierInboundHistoryResponseDto
        {
            InboundOrderNumber = io.InboundOrderNumber,
            PurchaseOrderNumber = io.PurchaseOrder?.PurchaseOrderNumber,
            SupplierName = io.PurchaseOrder?.Supplier?.SupplierName ?? "",
            WarehouseName = io.Warehouse?.WarehouseName,
            Status = io.Status,
            CreatedAt = io.CreatedAt,
            TotalQuantity = (int)io.InboundOrderItems.Sum(d => 
                io.Status == "COMPLETED" ? d.ReceivedQuantity : d.ExpectedQuantity)
        }).ToList();

        return new PagedResultDto<SupplierInboundHistoryResponseDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize
        };
    }

    public async Task<SupplierInboundHistoryDetailDto?> GetSupplierInboundHistoryDetailAsync(string supplierCode, string inboundOrderNumber)
    {
        var supplier = await _supplierRepository.GetSupplierByCodeAsync(supplierCode);
        if (supplier == null) return null;

        var order = await _supplierRepository.GetSupplierInboundHistoryDetailAsync(supplier.SupplierId, inboundOrderNumber);
        if (order == null) return null;

        return new SupplierInboundHistoryDetailDto
        {
            InboundOrderNumber = order.InboundOrderNumber,
            PurchaseOrderNumber = order.PurchaseOrder?.PurchaseOrderNumber,
            SupplierName = order.PurchaseOrder?.Supplier?.SupplierName ?? "",
            WarehouseName = order.Warehouse?.WarehouseName,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            ExpectedDate = order.ExpectedReceiptDate.ToDateTime(TimeOnly.MinValue),
            ReceiptDate = order.ConfirmedAt,
            Items = order.InboundOrderItems.Select(d => new SupplierInboundHistoryItemDto
            {
                ProductCode = d.Product.ProductCode,
                ProductName = d.Product.ProductName,
                UnitName = d.Product.UnitOfMeasure.UnitName,
                ExpectedQuantity = (int)d.ExpectedQuantity,
                ReceivedQuantity = (int)d.ReceivedQuantity
            }).ToList()
        };
    }
}
