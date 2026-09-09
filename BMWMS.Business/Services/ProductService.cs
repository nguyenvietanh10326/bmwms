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
            await NormalizeAndValidateAttributeValuesAsync(dto.ProductGroupId, dto.AttributeValues);
            string cleanCode = dto.ProductCode.Trim().ToUpper();

            if (await _productRepository.IsCodeExistsAsync(cleanCode))
            {
                throw new InvalidOperationException($"Mã sản phẩm '{cleanCode}' đã tồn tại trong hệ thống.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Barcode) && await _productRepository.IsBarcodeExistsAsync(dto.Barcode.Trim()))
            {
                throw new InvalidOperationException($"Mã vạch '{dto.Barcode.Trim()}' đã được sử dụng bởi sản phẩm khác.");
            }

            // Kiểm tra quy tắc nhóm hàng (PG06, PG07, PG08 -> FEFO & Lô/Hạn)
            var group = await _productGroupRepository.GetByIdAsync(dto.ProductGroupId);
            if (group != null)
            {
                if (group.GroupCode == "PG06" || group.GroupCode == "PG07" || group.GroupCode == "PG08")
                {
                    dto.RotationMethod = "FEFO";
                    dto.TrackExpiry = true;
                }
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
                NewValues = new { product.ProductCode, product.ProductName, product.RotationMethod, product.Status }
            });

            return id;
        }

        public async Task UpdateAsync(long productId, UpdateProductDto dto, long currentUserId)
        {
            await NormalizeAndValidateAttributeValuesAsync(dto.ProductGroupId, dto.AttributeValues);
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy sản phẩm với ID = {productId}.");
            }

            var oldValues = new { product.ProductCode, product.ProductName, product.RotationMethod, product.Status };

            string cleanCode = dto.ProductCode.Trim().ToUpper();
            if (await _productRepository.IsCodeExistsAsync(cleanCode, productId))
            {
                throw new InvalidOperationException($"Mã sản phẩm '{cleanCode}' đã được sử dụng bởi sản phẩm khác.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Barcode) && await _productRepository.IsBarcodeExistsAsync(dto.Barcode.Trim(), productId))
            {
                throw new InvalidOperationException($"Mã vạch '{dto.Barcode.Trim()}' đã được sử dụng bởi sản phẩm khác.");
            }

            // Kiểm tra quy tắc nhóm hàng
            var group = await _productGroupRepository.GetByIdAsync(dto.ProductGroupId);
            if (group != null)
            {
                if (group.GroupCode == "PG06" || group.GroupCode == "PG07" || group.GroupCode == "PG08")
                {
                    dto.RotationMethod = "FEFO";
                    dto.TrackExpiry = true;
                }
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
                NewValues = new { product.ProductCode, product.ProductName, product.RotationMethod, product.Status }
            });
        }

        private async Task NormalizeAndValidateAttributeValuesAsync(
            long productGroupId,
            List<ProductAttributeValueDto>? values)
        {
            var submitted = values?
                .Where(value => !string.IsNullOrWhiteSpace(value.AttributeValue))
                .ToList() ?? new List<ProductAttributeValueDto>();
            if (submitted.GroupBy(value => value.ProductAttributeId).Any(group => group.Count() > 1))
                throw new InvalidOperationException("Không được gửi lặp cùng một thuộc tính sản phẩm.");

            var productGroup = await _productGroupRepository.GetByIdAsync(productGroupId, includeAttributes: true);
            if (productGroup == null)
                throw new InvalidOperationException("Product group does not exist or is inactive.");

            var configuredAttributes = productGroup.ProductGroupAttributes
                .Where(groupAttribute => groupAttribute.ProductAttribute.Status == "ACTIVE")
                .ToDictionary(
                    groupAttribute => groupAttribute.ProductAttribute.AttributeCode,
                    StringComparer.OrdinalIgnoreCase);

            if (!configuredAttributes.TryGetValue(
                    CapacityEvaluationService.StorageVolumeAttributeCode,
                    out var storageVolumeAttribute))
            {
                throw new InvalidOperationException(
                    "The product group must configure STORAGE_VOLUME_M3_PER_BASE_UOM before products can be saved.");
            }

            var storageVolumeValue = submitted.FirstOrDefault(value =>
                value.ProductAttributeId == storageVolumeAttribute.ProductAttributeId);
            if (storageVolumeValue == null)
            {
                throw new InvalidOperationException(
                    "Storage volume conversion (m3 per base unit) is required for capacity calculation.");
            }

            var attributes = await _productRepository.GetProductAttributesByIdsAsync(
                submitted.Select(value => value.ProductAttributeId));
            var attributesById = attributes.ToDictionary(attribute => attribute.ProductAttributeId);
            if (attributesById.Count != submitted.Count)
                throw new InvalidOperationException("Một hoặc nhiều thuộc tính sản phẩm không tồn tại.");

            foreach (var submittedValue in submitted)
            {
                var attribute = attributesById[submittedValue.ProductAttributeId];
                if (!string.Equals(attribute.DataType, "NUMBER", StringComparison.OrdinalIgnoreCase))
                    continue;

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

                submittedValue.AttributeValue = number.ToString(CultureInfo.InvariantCulture);
            }
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
