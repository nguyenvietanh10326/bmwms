using BMWMS.Business.DTOs.ProductAttribute;
using BMWMS.Repository.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using BMWMS.Repository.Models;

namespace BMWMS.Business.Services;

public class ProductAttributeService : IProductAttributeService
{
    private readonly IProductAttributeRepository _repository;

    public ProductAttributeService(IProductAttributeRepository repository)
    {
        _repository = repository;
    }

    public async Task<BMWMS.Business.Common.PagedResultDto<ProductAttributeDto>> GetPagedAsync(string? keyword, string? status, int pageIndex, int pageSize)
    {
        if (pageIndex < 1) pageIndex = 1;
        if (pageSize < 1) pageSize = 20;

        var (items, totalCount) = await _repository.GetPagedAsync(keyword, status, pageIndex, pageSize);
        
        return new BMWMS.Business.Common.PagedResultDto<ProductAttributeDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<List<ProductAttributeDto>> GetAllAsync()
    {
        var attributes = await _repository.GetAllAsync();
        return attributes.Select(MapToDto).ToList();
    }

    public async Task<ProductAttributeDto?> GetByIdAsync(long id)
    {
        var attribute = await _repository.GetByIdAsync(id);
        if (attribute == null) return null;
        return MapToDto(attribute);
    }

    public async Task<ProductAttributeDto> CreateAsync(CreateProductAttributeDto dto)
    {
        var attribute = new BMWMS.Repository.Models.ProductAttribute
        {
            AttributeCode = dto.AttributeCode.ToUpper(),
            AttributeName = dto.AttributeName,
            DataType = dto.DataType,
            UnitLabel = dto.UnitLabel,
            Description = dto.Description,
            Status = dto.Status,
            ProductAttributeOptions = dto.Options.Select(o => new ProductAttributeOption
            {
                OptionCode = o.OptionCode,
                OptionValue = o.OptionValue,
                DisplayOrder = o.DisplayOrder,
                IsActive = o.IsActive
            }).ToList()
        };

        var created = await _repository.AddAsync(attribute);
        return MapToDto(created);
    }

    public async Task UpdateAsync(long id, UpdateProductAttributeDto dto)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing is null)
            throw new InvalidOperationException("Không tìm thấy thuộc tính sản phẩm.");
        if (await _repository.IsAttributeUsedAsync(id))
        {
            var oldOptions = existing.ProductAttributeOptions
                .OrderBy(option => option.ProductAttributeOptionId)
                .Select(option => (option.ProductAttributeOptionId, option.OptionCode, option.IsActive))
                .ToList();
            var newOptions = dto.Options
                .OrderBy(option => option.ProductAttributeOptionId)
                .Select(option => (option.ProductAttributeOptionId, option.OptionCode, option.IsActive))
                .ToList();
            if (!string.Equals(existing.AttributeCode, dto.AttributeCode, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(existing.DataType, dto.DataType, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(existing.UnitLabel, dto.UnitLabel, StringComparison.OrdinalIgnoreCase) ||
                !oldOptions.SequenceEqual(newOptions))
                throw new InvalidOperationException(
                    "Thuộc tính đang được dùng: không đổi mã, kiểu dữ liệu, ĐVT hoặc mã lựa chọn. Có thể đổi tên/mô tả hoặc ngừng dùng thuộc tính.");
        }

        var attribute = new BMWMS.Repository.Models.ProductAttribute
        {
            ProductAttributeId = id,
            AttributeCode = dto.AttributeCode.ToUpper(),
            AttributeName = dto.AttributeName,
            DataType = dto.DataType,
            UnitLabel = dto.UnitLabel,
            Description = dto.Description,
            Status = dto.Status,
            ProductAttributeOptions = dto.Options.Select(o => new ProductAttributeOption
            {
                ProductAttributeOptionId = o.ProductAttributeOptionId,
                OptionCode = o.OptionCode,
                OptionValue = o.OptionValue,
                DisplayOrder = o.DisplayOrder,
                IsActive = o.IsActive
            }).ToList()
        };

        await _repository.UpdateAsync(attribute);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        bool isUsed = await _repository.IsAttributeUsedAsync(id);
        if (isUsed)
        {
            throw new InvalidOperationException("Không thể xóa thông số kỹ thuật này vì nó đang được sử dụng trong Nhóm sản phẩm hoặc Sản phẩm. Hãy chuyển trạng thái sang INACTIVE.");
        }
        
        return await _repository.DeleteAsync(id);
    }

    private static ProductAttributeDto MapToDto(BMWMS.Repository.Models.ProductAttribute attribute)
    {
        return new ProductAttributeDto
        {
            ProductAttributeId = attribute.ProductAttributeId,
            AttributeCode = attribute.AttributeCode,
            AttributeName = attribute.AttributeName,
            DataType = attribute.DataType,
            UnitLabel = attribute.UnitLabel,
            Description = attribute.Description,
            Status = attribute.Status,
            Options = attribute.ProductAttributeOptions.Select(o => new ProductAttributeOptionDto
            {
                ProductAttributeOptionId = o.ProductAttributeOptionId,
                ProductAttributeId = o.ProductAttributeId,
                OptionCode = o.OptionCode,
                OptionValue = o.OptionValue,
                DisplayOrder = o.DisplayOrder,
                IsActive = o.IsActive
            }).OrderBy(o => o.DisplayOrder).ToList()
        };
    }
}
