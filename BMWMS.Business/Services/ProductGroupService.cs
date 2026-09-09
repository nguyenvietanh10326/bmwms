using BMWMS.Business.Common;
using BMWMS.Business.DTOs.Audit;
using BMWMS.Business.Services;
using BMWMS.Business.DTOs.ProductGroup;
using BMWMS.Business.Interfaces;
using BMWMS.Repository.Interfaces;
using BMWMS.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BMWMS.Business.Services
{
    public class ProductGroupService : IProductGroupService
    {
        private const string StorageVolumeAttributeCode = "STORAGE_VOLUME_M3_PER_BASE_UOM";

        private readonly IProductGroupRepository _productGroupRepository;
        private readonly IAuditLogService _auditLogService;

        public ProductGroupService(IProductGroupRepository productGroupRepository, IAuditLogService auditLogService)
        {
            _productGroupRepository = productGroupRepository;
            _auditLogService = auditLogService;
        }

        public async Task<PagedResultDto<ProductGroupResponseDto>> GetPagedListAsync(ProductGroupFilterDto filter)
        {
            int pageIndex = filter.PageIndex < 1 ? 1 : filter.PageIndex;
            int pageSize = filter.PageSize < 1 ? 20 : (filter.PageSize > 100 ? 100 : filter.PageSize);

            var (items, totalCount) = await _productGroupRepository.GetPagedListAsync(
                filter.Keyword,
                filter.Status,
                pageIndex,
                pageSize);

            var dtos = items.Select(g => new ProductGroupResponseDto
            {
                ProductGroupId = g.ProductGroupId,
                GroupCode = g.GroupCode,
                GroupName = g.GroupName,
                Description = g.Description,
                Status = g.Status,
                ProductCount = g.Products.Count,
                AttributeCount = g.ProductGroupAttributes.Count,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            }).ToList();

            return new PagedResultDto<ProductGroupResponseDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        public async Task<List<ProductGroupResponseDto>> GetAllActiveAsync()
        {
            var items = await _productGroupRepository.GetAllActiveAsync();
            return items.Select(g => new ProductGroupResponseDto
            {
                ProductGroupId = g.ProductGroupId,
                GroupCode = g.GroupCode,
                GroupName = g.GroupName,
                Description = g.Description,
                Status = g.Status,
                ProductCount = g.Products.Count,
                AttributeCount = g.ProductGroupAttributes.Count,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            }).ToList();
        }

        public async Task<ProductGroupDetailDto?> GetByIdAsync(long productGroupId)
        {
            var group = await _productGroupRepository.GetByIdAsync(productGroupId, includeAttributes: true);
            if (group == null) return null;

            return new ProductGroupDetailDto
            {
                ProductGroupId = group.ProductGroupId,
                GroupCode = group.GroupCode,
                GroupName = group.GroupName,
                Description = group.Description,
                Status = group.Status,
                ProductCount = group.Products.Count,
                AttributeCount = group.ProductGroupAttributes.Count,
                CreatedAt = group.CreatedAt,
                UpdatedAt = group.UpdatedAt,
                Attributes = group.ProductGroupAttributes.Select(pga => new GroupAttributeConfigDto
                {
                    ProductAttributeId = pga.ProductAttributeId,
                    AttributeCode = pga.ProductAttribute.AttributeCode,
                    AttributeName = pga.ProductAttribute.AttributeName,
                    DataType = pga.ProductAttribute.DataType,
                    UnitLabel = pga.ProductAttribute.UnitLabel,
                    Description = pga.ProductAttribute.Description,
                    IsRequired = pga.IsRequired,
                    DisplayOrder = pga.DisplayOrder,
                    DefaultValue = pga.DefaultValue,
                    Options = pga.ProductAttribute.ProductAttributeOptions.Select(o => new ProductAttributeOptionDto
                    {
                        ProductAttributeOptionId = o.ProductAttributeOptionId,
                        ProductAttributeId = o.ProductAttributeId,
                        OptionCode = o.OptionCode,
                        OptionValue = o.OptionValue,
                        DisplayOrder = o.DisplayOrder,
                        IsActive = o.IsActive
                    }).ToList()
                }).ToList()
            };
        }

        public async Task<List<GroupAttributeConfigDto>> GetAttributesByGroupIdAsync(long productGroupId)
        {
            var groupAttrs = await _productGroupRepository.GetAttributesByGroupIdAsync(productGroupId);
            return groupAttrs.Select(pga => new GroupAttributeConfigDto
            {
                ProductAttributeId = pga.ProductAttributeId,
                AttributeCode = pga.ProductAttribute.AttributeCode,
                AttributeName = pga.ProductAttribute.AttributeName,
                DataType = pga.ProductAttribute.DataType,
                UnitLabel = pga.ProductAttribute.UnitLabel,
                Description = pga.ProductAttribute.Description,
                IsRequired = pga.IsRequired,
                DisplayOrder = pga.DisplayOrder,
                DefaultValue = pga.DefaultValue,
                Options = pga.ProductAttribute.ProductAttributeOptions.Select(o => new ProductAttributeOptionDto
                {
                    ProductAttributeOptionId = o.ProductAttributeOptionId,
                    ProductAttributeId = o.ProductAttributeId,
                    OptionCode = o.OptionCode,
                    OptionValue = o.OptionValue,
                    DisplayOrder = o.DisplayOrder,
                    IsActive = o.IsActive
                }).ToList()
            }).ToList();
        }

        public async Task<List<ProductAttributeDto>> GetAllAttributesAsync()
        {
            var attrs = await _productGroupRepository.GetAllAttributesAsync();
            return attrs.Select(a => new ProductAttributeDto
            {
                ProductAttributeId = a.ProductAttributeId,
                AttributeCode = a.AttributeCode,
                AttributeName = a.AttributeName,
                DataType = a.DataType,
                UnitLabel = a.UnitLabel,
                Description = a.Description,
                Status = a.Status,
                Options = a.ProductAttributeOptions.Select(o => new ProductAttributeOptionDto
                {
                    ProductAttributeOptionId = o.ProductAttributeOptionId,
                    ProductAttributeId = o.ProductAttributeId,
                    OptionCode = o.OptionCode,
                    OptionValue = o.OptionValue,
                    DisplayOrder = o.DisplayOrder,
                    IsActive = o.IsActive
                }).ToList()
            }).ToList();
        }

        public async Task<long> CreateAsync(CreateProductGroupDto dto, long userId)
        {
            var isCodeExists = await _productGroupRepository.IsGroupCodeExistsAsync(dto.GroupCode.Trim());
            if (isCodeExists)
                throw new InvalidOperationException($"Ma nhom san pham '{dto.GroupCode}' da ton tai trong he thong.");

            var group = new ProductGroup
            {
                GroupCode = dto.GroupCode.Trim().ToUpper(),
                GroupName = dto.GroupName.Trim(),
                Description = dto.Description?.Trim(),
                Status = dto.Status ?? "ACTIVE",
                CreatedAt = DateTime.UtcNow
            };

            var id = await _productGroupRepository.AddAsync(group);

            var attrs = await BuildValidatedGroupAttributesAsync(
                id,
                dto.Attributes ?? new List<GroupAttributeAssignmentDto>());
            await _productGroupRepository.UpdateGroupAttributesAsync(id, attrs);

            await _auditLogService.RecordAsync(new AuditEventDto
            {
                UserId = userId > 0 ? userId : null,
                ActionType = AuditActions.Create,
                EntityName = AuditEntities.ProductGroup,
                EntityId = id.ToString(),
                NewValues = new { group.GroupCode, group.GroupName, group.Description, group.Status }
            });

            return id;
        }

        public async Task UpdateAsync(long productGroupId, UpdateProductGroupDto dto, long userId)
        {
            var group = await _productGroupRepository.GetByIdAsync(productGroupId);
            if (group == null)
                throw new KeyNotFoundException($"Khong tim thay nhom san pham voi ID: {productGroupId}");

            var oldValues = new { group.GroupName, group.Description, group.Status };

            group.GroupName = dto.GroupName.Trim();
            group.Description = dto.Description?.Trim();
            group.Status = dto.Status;
            group.UpdatedAt = DateTime.UtcNow;

            await _productGroupRepository.UpdateAsync(group);

            if (dto.Attributes != null)
            {
                var attrs = await BuildValidatedGroupAttributesAsync(productGroupId, dto.Attributes);
                await _productGroupRepository.UpdateGroupAttributesAsync(productGroupId, attrs);
            }

            await _auditLogService.RecordAsync(new AuditEventDto
            {
                UserId = userId > 0 ? userId : null,
                ActionType = AuditActions.Update,
                EntityName = AuditEntities.ProductGroup,
                EntityId = productGroupId.ToString(),
                OldValues = oldValues,
                NewValues = new { group.GroupName, group.Description, group.Status }
            });
        }

        public async Task ToggleStatusAsync(long productGroupId, long userId)
        {
            var group = await _productGroupRepository.GetByIdAsync(productGroupId);
            if (group == null)
                throw new KeyNotFoundException($"Khong tim thay nhom san pham voi ID: {productGroupId}");

            var oldStatus = group.Status;
            group.Status = group.Status == "ACTIVE" ? "INACTIVE" : "ACTIVE";
            group.UpdatedAt = DateTime.UtcNow;

            await _productGroupRepository.UpdateAsync(group);

            await _auditLogService.RecordAsync(new AuditEventDto
            {
                UserId = userId > 0 ? userId : null,
                ActionType = AuditActions.ChangeStatus,
                EntityName = AuditEntities.ProductGroup,
                EntityId = productGroupId.ToString(),
                OldValues = new { Status = oldStatus },
                NewValues = new { Status = group.Status }
            });
        }

        public async Task DeleteAsync(long productGroupId, long userId)
        {
            var group = await _productGroupRepository.GetByIdAsync(productGroupId);
            if (group == null)
                throw new KeyNotFoundException($"Khong tim thay nhom san pham voi ID: {productGroupId}");

            var hasProducts = await _productGroupRepository.HasProductsAsync(productGroupId);
            if (hasProducts)
                throw new InvalidOperationException("Khong the xoa nhom san pham nay vi dang co san pham/vat tu lien ket.");

            var snapshot = new { group.GroupCode, group.GroupName, group.Description, group.Status };

            await _productGroupRepository.DeleteAsync(productGroupId);

            await _auditLogService.RecordAsync(new AuditEventDto
            {
                UserId = userId > 0 ? userId : null,
                ActionType = AuditActions.Delete,
                EntityName = AuditEntities.ProductGroup,
                EntityId = productGroupId.ToString(),
                OldValues = snapshot
            });
        }

        public async Task UpdateGroupAttributesAsync(long productGroupId, List<GroupAttributeAssignmentDto> attributes, long userId)
        {
            var group = await _productGroupRepository.GetByIdAsync(productGroupId);
            if (group == null)
                throw new KeyNotFoundException($"Khong tim thay nhom san pham voi ID: {productGroupId}");

            var attrs = await BuildValidatedGroupAttributesAsync(productGroupId, attributes);

            await _productGroupRepository.UpdateGroupAttributesAsync(productGroupId, attrs);

            await _auditLogService.RecordAsync(new AuditEventDto
            {
                UserId = userId > 0 ? userId : null,
                ActionType = AuditActions.Update,
                EntityName = AuditEntities.ProductGroup,
                EntityId = productGroupId.ToString(),
                NewValues = new
                {
                    GroupCode = group.GroupCode,
                    Action = "Cap nhat thuoc tinh EAV",
                    AttributeCount = attributes.Count
                }
            });
        }

        private async Task<List<ProductGroupAttribute>> BuildValidatedGroupAttributesAsync(
            long productGroupId,
            IEnumerable<GroupAttributeAssignmentDto> assignments)
        {
            var allAttributes = await _productGroupRepository.GetAllAttributesAsync();
            var storageVolumeAttribute = allAttributes.FirstOrDefault(attribute =>
                string.Equals(attribute.AttributeCode, StorageVolumeAttributeCode, StringComparison.OrdinalIgnoreCase));
            if (storageVolumeAttribute == null)
            {
                throw new InvalidOperationException(
                    "System attribute STORAGE_VOLUME_M3_PER_BASE_UOM is missing. Run the capacity schema patch first.");
            }

            var submitted = assignments.ToList();
            var capacityAssignment = submitted.FirstOrDefault(attribute =>
                attribute.ProductAttributeId == storageVolumeAttribute.ProductAttributeId);
            if (capacityAssignment == null)
            {
                submitted.Add(new GroupAttributeAssignmentDto
                {
                    ProductAttributeId = storageVolumeAttribute.ProductAttributeId,
                    IsRequired = true,
                    DisplayOrder = submitted.Count == 0
                        ? 1
                        : submitted.Max(attribute => attribute.DisplayOrder) + 1
                });
            }
            else
            {
                capacityAssignment.IsRequired = true;
                capacityAssignment.DefaultValue = null;
            }

            return submitted
                .GroupBy(attribute => attribute.ProductAttributeId)
                .Select(group =>
                {
                    var assignment = group.First();
                    return new ProductGroupAttribute
                    {
                        ProductGroupId = productGroupId,
                        ProductAttributeId = assignment.ProductAttributeId,
                        IsRequired = assignment.ProductAttributeId == storageVolumeAttribute.ProductAttributeId
                            || assignment.IsRequired,
                        DisplayOrder = assignment.DisplayOrder,
                        DefaultValue = assignment.ProductAttributeId == storageVolumeAttribute.ProductAttributeId
                            ? null
                            : assignment.DefaultValue
                    };
                })
                .ToList();
        }
    }
}
