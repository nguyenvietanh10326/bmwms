using System.Text.RegularExpressions;
using BMWMS.Business.Common;
using BMWMS.Business.DTOs.ProductAttribute;
using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;

namespace BMWMS.Business.Services;

public class ProductAttributeService : IProductAttributeService
{
    private static readonly HashSet<string> AllowedDataTypes = ["TEXT", "NUMBER", "DATE", "BOOLEAN", "OPTION"];
    private static readonly HashSet<string> AllowedStatuses = ["ACTIVE", "INACTIVE"];
    private readonly IProductAttributeRepository _repository;

    public ProductAttributeService(IProductAttributeRepository repository) => _repository = repository;

    public async Task<PagedResultDto<ProductAttributeDto>> GetPagedAsync(
        string? keyword, string? status, int pageIndex, int pageSize)
    {
        pageIndex = Math.Max(1, pageIndex);
        pageSize = Math.Max(1, pageSize);
        var (items, totalCount) = await _repository.GetPagedAsync(keyword, status, pageIndex, pageSize);
        return new PagedResultDto<ProductAttributeDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<List<ProductAttributeDto>> GetAllAsync() =>
        (await _repository.GetAllAsync()).Select(MapToDto).ToList();

    public async Task<ProductAttributeDto?> GetByIdAsync(long id)
    {
        var attribute = await _repository.GetByIdAsync(id);
        return attribute == null ? null : MapToDto(attribute);
    }

    public async Task<ProductAttributeDto> CreateAsync(CreateProductAttributeDto dto)
    {
        await ValidateAsync(dto.AttributeCode, dto.AttributeName, dto.DataType, dto.Status, dto.Options);
        var attribute = ToEntity(dto.AttributeCode, dto.AttributeName, dto.DataType, dto.UnitLabel,
            dto.Description, dto.Status, dto.Options);
        return MapToDto(await _repository.AddAsync(attribute));
    }

    public async Task UpdateAsync(long id, UpdateProductAttributeDto dto)
    {
        var existing = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Không tìm thấy thuộc tính sản phẩm.");
        await ValidateAsync(dto.AttributeCode, dto.AttributeName, dto.DataType, dto.Status, dto.Options, id);

        if (await _repository.IsAttributeUsedAsync(id))
        {
            var oldOptions = existing.ProductAttributeOptions.OrderBy(option => option.ProductAttributeOptionId)
                .Select(option => (option.ProductAttributeOptionId, option.OptionCode, option.IsActive)).ToList();
            var newOptions = dto.Options.OrderBy(option => option.ProductAttributeOptionId)
                .Select(option => (option.ProductAttributeOptionId, option.OptionCode, option.IsActive)).ToList();
            if (!string.Equals(existing.AttributeCode, dto.AttributeCode, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(existing.DataType, dto.DataType, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(existing.UnitLabel, dto.UnitLabel, StringComparison.OrdinalIgnoreCase) ||
                !oldOptions.SequenceEqual(newOptions))
                throw new InvalidOperationException(
                    "Thuộc tính đang được sử dụng; không thể đổi mã, kiểu dữ liệu, đơn vị hoặc mã lựa chọn.");
        }

        var attribute = ToEntity(dto.AttributeCode, dto.AttributeName, dto.DataType, dto.UnitLabel,
            dto.Description, dto.Status, dto.Options);
        attribute.ProductAttributeId = id;
        await _repository.UpdateAsync(attribute);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        if (await _repository.IsAttributeUsedAsync(id))
            throw new InvalidOperationException(
                "Không thể xóa thuộc tính đang được sử dụng. Hãy chuyển trạng thái sang không hoạt động.");
        return await _repository.DeleteAsync(id);
    }

    private async Task ValidateAsync(
        string? code, string? name, string? dataType, string? status,
        List<ProductAttributeOptionDto>? options, long? excludeId = null)
    {
        var normalizedCode = code?.Trim().ToUpperInvariant() ?? string.Empty;
        var normalizedType = dataType?.Trim().ToUpperInvariant() ?? string.Empty;
        var normalizedStatus = status?.Trim().ToUpperInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedCode) || string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Mã và tên thuộc tính không được để trống.");
        if (!Regex.IsMatch(normalizedCode, "^[A-Z0-9_]+$"))
            throw new InvalidOperationException("Mã thuộc tính chỉ gồm chữ in hoa, số và dấu gạch dưới.");
        if (await _repository.IsCodeExistsAsync(normalizedCode, excludeId))
            throw new InvalidOperationException($"Mã thuộc tính '{normalizedCode}' đã tồn tại.");
        if (!AllowedDataTypes.Contains(normalizedType))
            throw new InvalidOperationException("Kiểu dữ liệu thuộc tính không hợp lệ.");
        if (!AllowedStatuses.Contains(normalizedStatus))
            throw new InvalidOperationException("Trạng thái thuộc tính không hợp lệ.");

        var submitted = options ?? [];
        if (normalizedType == "OPTION" && submitted.Count == 0)
            throw new InvalidOperationException("Thuộc tính lựa chọn phải có ít nhất một giá trị.");
        if (normalizedType != "OPTION" && submitted.Count > 0)
            throw new InvalidOperationException("Chỉ thuộc tính kiểu lựa chọn mới được cấu hình danh sách giá trị.");
        if (submitted.Any(option => string.IsNullOrWhiteSpace(option.OptionCode) || string.IsNullOrWhiteSpace(option.OptionValue)))
            throw new InvalidOperationException("Mã và giá trị lựa chọn không được để trống.");
        if (submitted.GroupBy(option => option.OptionCode.Trim(), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            throw new InvalidOperationException("Mã lựa chọn không được trùng nhau.");
        if (submitted.Any(option => option.DisplayOrder < 0))
            throw new InvalidOperationException("Thứ tự hiển thị không được âm.");
    }

    private static ProductAttribute ToEntity(
        string code, string name, string dataType, string? unitLabel, string? description,
        string status, IEnumerable<ProductAttributeOptionDto> options) => new()
    {
        AttributeCode = code.Trim().ToUpperInvariant(),
        AttributeName = name.Trim(),
        DataType = dataType.Trim().ToUpperInvariant(),
        UnitLabel = unitLabel?.Trim(),
        Description = description?.Trim(),
        Status = status.Trim().ToUpperInvariant(),
        ProductAttributeOptions = options.Select(option => new ProductAttributeOption
        {
            ProductAttributeOptionId = option.ProductAttributeOptionId,
            OptionCode = option.OptionCode.Trim().ToUpperInvariant(),
            OptionValue = option.OptionValue.Trim(),
            DisplayOrder = option.DisplayOrder,
            IsActive = option.IsActive
        }).ToList()
    };

    private static ProductAttributeDto MapToDto(ProductAttribute attribute) => new()
    {
        ProductAttributeId = attribute.ProductAttributeId,
        AttributeCode = attribute.AttributeCode,
        AttributeName = attribute.AttributeName,
        DataType = attribute.DataType,
        UnitLabel = attribute.UnitLabel,
        Description = attribute.Description,
        Status = attribute.Status,
        Options = attribute.ProductAttributeOptions.OrderBy(option => option.DisplayOrder)
            .Select(option => new ProductAttributeOptionDto
            {
                ProductAttributeOptionId = option.ProductAttributeOptionId,
                ProductAttributeId = option.ProductAttributeId,
                OptionCode = option.OptionCode,
                OptionValue = option.OptionValue,
                DisplayOrder = option.DisplayOrder,
                IsActive = option.IsActive
            }).ToList()
    };
}
