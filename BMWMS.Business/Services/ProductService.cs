using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Audit;
using BMWMS.Business.DTOs.Product;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;

namespace BMWMS.Business.Services
{
    public class ProductService : IProductService
    {
        private const decimal MaximumStorageFactor = 1_000_000m;
        private const int MaximumStorageFactorDecimalPlaces = 8;

        private readonly IProductRepository _productRepository;
        private readonly IProductGroupRepository _productGroupRepository;
        private readonly IAuditLogService _auditLogService;

        public ProductService(IProductRepository productRepository, IProductGroupRepository productGroupRepository, IAuditLogService auditLogService)
        {
            _productRepository = productRepository;
            _productGroupRepository = productGroupRepository;
            _auditLogService = auditLogService;
        }

        public async Task<PagedResultDto<ProductResponseDto>> GetPagedListAsync(ProductFilterDto filter)
        {
            int pageIndex = filter.PageIndex < 1 ? 1 : filter.PageIndex;
            int pageSize = filter.PageSize < 1 ? 10 : (filter.PageSize > 100 ? 100 : filter.PageSize);

            var (items, totalCount) = await _productRepository.GetPagedListAsync(
                filter.Keyword,
                filter.ProductGroupId,
                filter.UnitOfMeasureId,
                filter.Status,
                filter.RotationMethod,
                pageIndex,
                pageSize);

            var dtos = items.Select(p => new ProductResponseDto
            {
                ProductId = p.ProductId,
                ProductCode = p.ProductCode,
                ProductName = p.ProductName,
                ProductGroupId = p.ProductGroupId,
                GroupName = p.ProductGroup?.GroupName ?? "N/A",
                UnitOfMeasureId = p.UnitOfMeasureId,
                UnitName = p.UnitOfMeasure?.UnitName ?? "N/A",
                QuantityScale = p.UnitOfMeasure?.QuantityScale ?? 0,
                Barcode = p.Barcode,
                Description = p.Description,
                RotationMethod = p.RotationMethod,
                TrackLot = p.TrackLot,
                TrackExpiry = p.TrackExpiry,
                DefaultShelfLifeDays = p.DefaultShelfLifeDays,
                Status = p.Status,
                TotalStockOnHand = p.Inventories?.Sum(i => i.OnHandQuantity) ?? 0,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();

            return new PagedResultDto<ProductResponseDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<ProductDetailResponseDto?> GetByIdAsync(long productId)
        {
            var product = await _productRepository.GetByIdAsync(productId, includeDetails: true);
            if (product == null) return null;

            var dto = new ProductDetailResponseDto
            {
                ProductId = product.ProductId,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                ProductGroupId = product.ProductGroupId,
                GroupName = product.ProductGroup?.GroupName ?? "N/A",
                UnitOfMeasureId = product.UnitOfMeasureId,
                UnitName = product.UnitOfMeasure?.UnitName ?? "N/A",
                QuantityScale = product.UnitOfMeasure?.QuantityScale ?? 0,
                Barcode = product.Barcode,
                Description = product.Description,
                RotationMethod = product.RotationMethod,
                TrackLot = product.TrackLot,
                TrackExpiry = product.TrackExpiry,
                DefaultShelfLifeDays = product.DefaultShelfLifeDays,
                Status = product.Status,
                TotalStockOnHand = product.Inventories?.Sum(i => i.OnHandQuantity) ?? 0,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                CreatedByName = product.CreatedByUser?.FullName ?? "System",
                UpdatedByName = product.UpdatedByUser?.FullName,
                AttributeValues = product.ProductAttributeValues?.Select(pav => new ProductAttributeValueDto
                {
                    ProductAttributeId = pav.ProductAttributeId,
                    AttributeCode = pav.ProductAttribute?.AttributeCode,
                    AttributeName = pav.ProductAttribute?.AttributeName,
                    DataType = pav.ProductAttribute?.DataType,
                    UnitLabel = pav.ProductAttribute?.UnitLabel,
                    AttributeValue = pav.AttributeValue,
                    DisplayOrder = pav.ProductAttribute?.ProductGroupAttributes
                        .FirstOrDefault(pga => pga.ProductGroupId == product.ProductGroupId)?.DisplayOrder ?? 0
                }).OrderBy(a => a.DisplayOrder).ToList() ?? new(),
                StockLocations = product.Inventories?.Select(inv => new ProductStockLocationDto
                {
                    WarehouseId = inv.StorageLocation?.WarehouseId ?? 0,
                    WarehouseName = inv.StorageLocation?.Warehouse?.WarehouseName ?? "N/A",
                    StorageLocationCode = inv.StorageLocation?.LocationCode ?? "Chưa chỉ định",
                    QuantityOnHand = inv.OnHandQuantity,
                    QuantityReserved = inv.ReservedQuantity
                }).ToList() ?? new(),
                WarehousePolicies = product.ProductWarehousePolicies?.Select(pwp => new ProductWarehousePolicyDto
                {
                    WarehouseId = pwp.WarehouseId,
                    WarehouseName = pwp.Warehouse?.WarehouseName ?? "N/A",
                    MinimumStockQuantity = pwp.MinimumStockQuantity,
                    ExpiryWarningDays = pwp.ExpiryWarningDays
                }).ToList() ?? new(),
                Suppliers = product.SupplierProducts?.Select(sp => new ProductSupplierDto
                {
                    SupplierId = sp.SupplierId,
                    SupplierCode = sp.Supplier?.SupplierCode ?? "N/A",
                    SupplierName = sp.Supplier?.SupplierName ?? "N/A",
                    SupplierProductCode = sp.SupplierProductCode,
                    LeadTimeDays = sp.LeadTimeDays
                }).ToList() ?? new()
            };

            return dto;
        }

        public async Task<List<UnitOfMeasureDto>> GetUnitsOfMeasureAsync()
        {
            var units = await _productRepository.GetUnitsOfMeasureAsync();
            return units.Select(u => new UnitOfMeasureDto
            {
                UnitOfMeasureId = u.UnitOfMeasureId,
                UnitCode = u.UnitCode,
                UnitName = u.UnitName,
                QuantityScale = u.QuantityScale
            }).ToList();
        }

        public async Task<List<ProductGroupOptionDto>> GetProductGroupsAsync()
        {
            var groups = await _productRepository.GetProductGroupsAsync();
            return groups.Select(g => new ProductGroupOptionDto
            {
                ProductGroupId = g.ProductGroupId,
                GroupCode = g.GroupCode,
                GroupName = g.GroupName
            }).ToList();
        }

        public async Task<long> CreateAsync(CreateProductDto dto, long currentUserId)
        {
            dto.AttributeValues ??= new List<ProductAttributeValueDto>();
            await NormalizeAndValidateAttributeValuesAsync(dto.ProductGroupId, dto.AttributeValues);
            if (!(await _productRepository.GetUnitsOfMeasureAsync())
                .Any(unit => unit.UnitOfMeasureId == dto.UnitOfMeasureId))
                throw new InvalidOperationException("Đơn vị tính cơ sở không tồn tại hoặc đang ngừng hoạt động.");
            string cleanCode = dto.ProductCode.Trim().ToUpper();

            if (await _productRepository.IsCodeExistsAsync(cleanCode))
            {
                throw new InvalidOperationException($"Mã sản phẩm '{cleanCode}' đã tồn tại trong hệ thống.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Barcode) && await _productRepository.IsBarcodeExistsAsync(dto.Barcode.Trim()))
            {
                throw new InvalidOperationException($"Mã vạch '{dto.Barcode.Trim()}' đã được sử dụng bởi sản phẩm khác.");
            }

            // Ràng buộc: FEFO bắt buộc TrackExpiry
            if (dto.RotationMethod == "FEFO" && !dto.TrackExpiry)
            {
                throw new InvalidOperationException("Phương pháp xuất kho FEFO bắt buộc phải bật 'Theo dõi Hạn sử dụng (Expiry Date)'.");
            }

            var product = new Product
            {
                ProductCode = cleanCode,
                ProductName = dto.ProductName.Trim(),
                ProductGroupId = dto.ProductGroupId,
                UnitOfMeasureId = dto.UnitOfMeasureId,
                Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                RotationMethod = dto.RotationMethod,
                TrackLot = dto.TrackExpiry,
                TrackExpiry = dto.TrackExpiry,
                DefaultShelfLifeDays = dto.DefaultShelfLifeDays,
                Status = dto.Status ?? "ACTIVE",
                CreatedByUserId = currentUserId > 0 ? currentUserId : 1,
                CreatedAt = DateTime.UtcNow
            };

            var attributeValues = dto.AttributeValues?
                .Where(av => !string.IsNullOrWhiteSpace(av.AttributeValue))
                .Select(av => new ProductAttributeValue
                {
                    ProductAttributeId = av.ProductAttributeId,
                    AttributeValue = av.AttributeValue.Trim()
                }).ToList();

            var id = await _productRepository.AddAsync(product, attributeValues);

            await _auditLogService.RecordAsync(new AuditEventDto
            {
                UserId = currentUserId > 0 ? currentUserId : null,
                ActionType = AuditActions.Create,
                EntityName = AuditEntities.Product,
                EntityId = id.ToString(),
                NewValues = new
                {
                    product.ProductCode,
                    product.ProductName,
                    product.ProductGroupId,
                    product.UnitOfMeasureId,
                    product.RotationMethod,
                    product.Status,
                    StorageVolumeM3PerBaseUom = GetAttributeValue(
                        dto.AttributeValues,
                        CapacityEvaluationService.StorageVolumeAttributeCode)
                }
            });

            return id;
        }

        public async Task UpdateAsync(long productId, UpdateProductDto dto, long currentUserId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy sản phẩm với ID = {productId}.");
            }

            dto.AttributeValues ??= new List<ProductAttributeValueDto>();
            await NormalizeAndValidateAttributeValuesAsync(dto.ProductGroupId, dto.AttributeValues);
            if (!(await _productRepository.GetUnitsOfMeasureAsync())
                .Any(unit => unit.UnitOfMeasureId == dto.UnitOfMeasureId))
                throw new InvalidOperationException("Đơn vị tính cơ sở không tồn tại hoặc đang ngừng hoạt động.");

            var oldStorageVolume = GetAttributeValue(
                product.ProductAttributeValues.Select(value => new ProductAttributeValueDto
                {
                    ProductAttributeId = value.ProductAttributeId,
                    AttributeCode = value.ProductAttribute?.AttributeCode,
                    AttributeValue = value.AttributeValue
                }),
                CapacityEvaluationService.StorageVolumeAttributeCode);
            var newStorageVolume = GetAttributeValue(
                dto.AttributeValues,
                CapacityEvaluationService.StorageVolumeAttributeCode);
            var hasHistory = await _productRepository.HasTransactionsOrInventoryAsync(productId);

            if (hasHistory && product.UnitOfMeasureId != dto.UnitOfMeasureId)
                throw new InvalidOperationException(
                    "Không thể đổi đơn vị tính cơ sở của sản phẩm đã phát sinh tồn kho hoặc giao dịch.");
            if (hasHistory && product.ProductGroupId != dto.ProductGroupId)
                throw new InvalidOperationException(
                    "Không thể đổi nhóm của sản phẩm đã phát sinh tồn kho hoặc giao dịch. Hãy tạo sản phẩm mới nếu thay đổi bản chất vật tư.");

            var capacityFactorChanged = !string.Equals(
                oldStorageVolume,
                newStorageVolume,
                StringComparison.Ordinal);
            if (hasHistory && capacityFactorChanged && string.IsNullOrWhiteSpace(dto.CapacityChangeReason))
                throw new InvalidOperationException(
                    "Sản phẩm đã phát sinh tồn kho hoặc giao dịch. Phải nhập lý do khi điều chỉnh hệ số thể tích lưu kho.");

            var oldValues = new
            {
                product.ProductCode,
                product.ProductName,
                product.ProductGroupId,
                product.UnitOfMeasureId,
                product.RotationMethod,
                product.Status,
                StorageVolumeM3PerBaseUom = oldStorageVolume
            };

            string cleanCode = dto.ProductCode.Trim().ToUpper();
            if (await _productRepository.IsCodeExistsAsync(cleanCode, productId))
            {
                throw new InvalidOperationException($"Mã sản phẩm '{cleanCode}' đã được sử dụng bởi sản phẩm khác.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Barcode) && await _productRepository.IsBarcodeExistsAsync(dto.Barcode.Trim(), productId))
            {
                throw new InvalidOperationException($"Mã vạch '{dto.Barcode.Trim()}' đã được sử dụng bởi sản phẩm khác.");
            }

            // Ràng buộc: FEFO bắt buộc TrackExpiry
            if (dto.RotationMethod == "FEFO" && !dto.TrackExpiry)
            {
                throw new InvalidOperationException("Phương pháp xuất kho FEFO bắt buộc phải bật 'Theo dõi Hạn sử dụng (Expiry Date)'.");
            }

            product.ProductCode = cleanCode;
            product.ProductName = dto.ProductName.Trim();
            product.ProductGroupId = dto.ProductGroupId;
            product.UnitOfMeasureId = dto.UnitOfMeasureId;
            product.Barcode = string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim();
            product.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            product.RotationMethod = dto.RotationMethod;
            // Lot tracking is managed internally by the inventory flow, not by product configuration.
            product.TrackLot = product.TrackLot || dto.TrackExpiry;
            product.TrackExpiry = dto.TrackExpiry;
            product.DefaultShelfLifeDays = dto.DefaultShelfLifeDays;
            product.Status = dto.Status;
            product.UpdatedByUserId = currentUserId > 0 ? currentUserId : 1;
            product.UpdatedAt = DateTime.UtcNow;

            var attributeValues = dto.AttributeValues?
                .Where(av => !string.IsNullOrWhiteSpace(av.AttributeValue))
                .Select(av => new ProductAttributeValue
                {
                    ProductId = productId,
                    ProductAttributeId = av.ProductAttributeId,
                    AttributeValue = av.AttributeValue.Trim()
                }).ToList();

            await _productRepository.UpdateAsync(product, attributeValues);

            await _auditLogService.RecordAsync(new AuditEventDto
            {
                UserId = currentUserId > 0 ? currentUserId : null,
                ActionType = AuditActions.Update,
                EntityName = AuditEntities.Product,
                EntityId = productId.ToString(),
                OldValues = oldValues,
                NewValues = new
                {
                    product.ProductCode,
                    product.ProductName,
                    product.ProductGroupId,
                    product.UnitOfMeasureId,
                    product.RotationMethod,
                    product.Status,
                    StorageVolumeM3PerBaseUom = newStorageVolume,
                    CapacityChangeReason = capacityFactorChanged
                        ? dto.CapacityChangeReason?.Trim()
                        : null
                }
            });
        }

        private async Task NormalizeAndValidateAttributeValuesAsync(
            long productGroupId,
            List<ProductAttributeValueDto> values)
        {
            if (values.Where(value => value.ProductAttributeId > 0)
                .GroupBy(value => value.ProductAttributeId)
                .Any(group => group.Count() > 1))
                throw new InvalidOperationException("Không được gửi lặp cùng một thuộc tính sản phẩm.");

            var productGroup = await _productGroupRepository.GetByIdAsync(productGroupId, includeAttributes: true);
            if (productGroup == null)
                throw new InvalidOperationException("Nhóm sản phẩm không tồn tại.");
            if (!string.Equals(productGroup.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Không thể lưu sản phẩm vào nhóm đang ngừng hoạt động.");

            var configuredAttributesById = productGroup.ProductGroupAttributes
                .Where(groupAttribute => groupAttribute.ProductAttribute.Status == "ACTIVE")
                .ToDictionary(
                    groupAttribute => groupAttribute.ProductAttributeId);
            var configuredAttributesByCode = configuredAttributesById.Values.ToDictionary(
                groupAttribute => groupAttribute.ProductAttribute.AttributeCode,
                StringComparer.OrdinalIgnoreCase);

            if (!configuredAttributesByCode.TryGetValue(
                    CapacityEvaluationService.StorageVolumeAttributeCode,
                    out var storageVolumeAttribute))
            {
                throw new InvalidOperationException(
                    "Nhóm sản phẩm chưa cấu hình hệ số thể tích lưu kho. Hãy chạy bản cập nhật dữ liệu sức chứa trước.");
            }

            foreach (var value in values.Where(value => !string.IsNullOrWhiteSpace(value.AttributeValue)))
            {
                if (!configuredAttributesById.ContainsKey(value.ProductAttributeId))
                    throw new InvalidOperationException(
                        "Một hoặc nhiều thuộc tính gửi lên không thuộc cấu hình của nhóm sản phẩm đã chọn.");
            }

            foreach (var requiredAttribute in configuredAttributesById.Values.Where(attribute => attribute.IsRequired))
            {
                var value = values.FirstOrDefault(item => item.ProductAttributeId == requiredAttribute.ProductAttributeId);
                if (value != null && !string.IsNullOrWhiteSpace(value.AttributeValue))
                    continue;

                if (string.IsNullOrWhiteSpace(requiredAttribute.DefaultValue))
                    throw new InvalidOperationException(
                        $"Thuộc tính '{requiredAttribute.ProductAttribute.AttributeName}' là bắt buộc.");

                if (value == null)
                {
                    values.Add(new ProductAttributeValueDto
                    {
                        ProductAttributeId = requiredAttribute.ProductAttributeId,
                        AttributeCode = requiredAttribute.ProductAttribute.AttributeCode,
                        AttributeName = requiredAttribute.ProductAttribute.AttributeName,
                        AttributeValue = requiredAttribute.DefaultValue
                    });
                }
                else
                {
                    value.AttributeValue = requiredAttribute.DefaultValue;
                }
            }

            var submitted = values
                .Where(value => !string.IsNullOrWhiteSpace(value.AttributeValue))
                .ToList();

            foreach (var submittedValue in submitted)
            {
                var groupAttribute = configuredAttributesById[submittedValue.ProductAttributeId];
                var attribute = groupAttribute.ProductAttribute;
                submittedValue.AttributeCode = attribute.AttributeCode;
                submittedValue.AttributeName = attribute.AttributeName;
                submittedValue.DataType = attribute.DataType;
                submittedValue.UnitLabel = attribute.UnitLabel;

                if (string.Equals(attribute.DataType, "OPTION", StringComparison.OrdinalIgnoreCase))
                {
                    var option = attribute.ProductAttributeOptions.FirstOrDefault(candidate =>
                        candidate.IsActive &&
                        (string.Equals(candidate.OptionCode, submittedValue.AttributeValue.Trim(), StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(candidate.OptionValue, submittedValue.AttributeValue.Trim(), StringComparison.OrdinalIgnoreCase)));
                    if (option == null)
                        throw new InvalidOperationException(
                            $"Giá trị của thuộc tính '{attribute.AttributeName}' không nằm trong danh sách cho phép.");

                    // Luu ma option on dinh; giao dien chi dung OptionValue de hien thi.
                    submittedValue.AttributeValue = option.OptionCode;
                    continue;
                }

                if (!string.Equals(attribute.DataType, "NUMBER", StringComparison.OrdinalIgnoreCase))
                {
                    submittedValue.AttributeValue = submittedValue.AttributeValue.Trim();
                    continue;
                }

                var normalized = submittedValue.AttributeValue.Trim().Replace(',', '.');
                if (!decimal.TryParse(
                        normalized,
                        NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                        CultureInfo.InvariantCulture,
                        out var number))
                    throw new InvalidOperationException($"Thuộc tính '{attribute.AttributeName}' phải là số hợp lệ.");

                var isCapacityAttribute = attribute.AttributeCode is
                    CapacityEvaluationService.StorageWeightAttributeCode or
                    CapacityEvaluationService.StorageVolumeAttributeCode;
                if (isCapacityAttribute && number <= 0)
                    throw new InvalidOperationException($"Thuộc tính '{attribute.AttributeName}' phải lớn hơn 0.");
                if (isCapacityAttribute && number > MaximumStorageFactor)
                    throw new InvalidOperationException(
                        $"Thuộc tính '{attribute.AttributeName}' không được vượt quá {MaximumStorageFactor.ToString(CultureInfo.InvariantCulture)}.");
                if (isCapacityAttribute && CountSignificantDecimalPlaces(normalized) > MaximumStorageFactorDecimalPlaces)
                    throw new InvalidOperationException(
                        $"Thuộc tính '{attribute.AttributeName}' chỉ được có tối đa {MaximumStorageFactorDecimalPlaces} chữ số thập phân.");

                submittedValue.AttributeValue = number.ToString(CultureInfo.InvariantCulture);
            }
        }

        private static int CountSignificantDecimalPlaces(string value)
        {
            var separatorIndex = value.IndexOf('.');
            return separatorIndex < 0
                ? 0
                : value[(separatorIndex + 1)..].TrimEnd('0').Length;
        }

        private static string? GetAttributeValue(
            IEnumerable<ProductAttributeValueDto>? values,
            string attributeCode)
        {
            if (values == null)
                return null;

            var value = values.FirstOrDefault(item =>
                string.Equals(item.AttributeCode, attributeCode, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(value?.AttributeValue))
                return null;

            var normalized = value.AttributeValue.Trim().Replace(',', '.');
            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
                ? number.ToString(CultureInfo.InvariantCulture)
                : normalized;
        }

        public async Task ToggleStatusAsync(long productId, long currentUserId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy sản phẩm với ID = {productId}.");
            }

            var oldStatus = product.Status;
            product.Status = product.Status == "ACTIVE" ? "INACTIVE" : "ACTIVE";
            product.UpdatedByUserId = currentUserId > 0 ? currentUserId : 1;
            product.UpdatedAt = DateTime.UtcNow;

            await _productRepository.UpdateAsync(product);

            await _auditLogService.RecordAsync(new AuditEventDto
            {
                UserId = currentUserId > 0 ? currentUserId : null,
                ActionType = AuditActions.ChangeStatus,
                EntityName = AuditEntities.Product,
                EntityId = productId.ToString(),
                OldValues = new { Status = oldStatus },
                NewValues = new { Status = product.Status }
            });
        }

        public async Task DeleteAsync(long productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy sản phẩm với ID = {productId}.");
            }

            bool hasStock = await _productRepository.HasTransactionsOrInventoryAsync(productId);
            if (hasStock)
            {
                throw new InvalidOperationException("Không thể xóa sản phẩm đã phát sinh tồn kho hoặc giao dịch nhập xuất. Hãy chuyển sang trạng thái INACTIVE.");
            }

            var snapshot = new { product.ProductCode, product.ProductName, product.Status };

            await _productRepository.DeleteAsync(productId);

            await _auditLogService.RecordAsync(new AuditEventDto
            {
                ActionType = AuditActions.Delete,
                EntityName = AuditEntities.Product,
                EntityId = productId.ToString(),
                OldValues = snapshot
            });
        }
    }
}
