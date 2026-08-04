using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BMWMS.Business.DTOs.ProductGroup
{
    public class ProductGroupResponseDto
    {
        public long ProductGroupId { get; set; }
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public int ProductCount { get; set; }
        public int AttributeCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ProductGroupDetailDto : ProductGroupResponseDto
    {
        public List<GroupAttributeConfigDto> Attributes { get; set; } = new();
    }

    public class CreateProductGroupDto
    {
        [Required(ErrorMessage = "Mã nhóm sản phẩm không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã nhóm tối đa 50 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Mã nhóm chỉ gồm chữ cái, số, gạch ngang hoặc gạch dưới.")]
        public string GroupCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên nhóm sản phẩm không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên nhóm tối đa 200 ký tự.")]
        public string GroupName { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
        public string? Description { get; set; }

        public string Status { get; set; } = "ACTIVE";

        public List<GroupAttributeAssignmentDto>? Attributes { get; set; }
    }

    public class UpdateProductGroupDto
    {
        [Required(ErrorMessage = "Tên nhóm sản phẩm không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên nhóm tối đa 200 ký tự.")]
        public string GroupName { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [RegularExpression(@"^(ACTIVE|INACTIVE)$", ErrorMessage = "Trạng thái chỉ nhận giá trị ACTIVE hoặc INACTIVE.")]
        public string Status { get; set; } = "ACTIVE";

        public List<GroupAttributeAssignmentDto>? Attributes { get; set; }
    }

    public class GroupAttributeConfigDto
    {
        public long ProductAttributeId { get; set; }
        public string AttributeCode { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string DataType { get; set; } = "TEXT"; // TEXT, NUMBER, DATE, BOOLEAN, OPTION
        public string? UnitLabel { get; set; }
        public string? Description { get; set; }
        public bool IsRequired { get; set; }
        public int DisplayOrder { get; set; }
        public string? DefaultValue { get; set; }
        public List<ProductAttributeOptionDto> Options { get; set; } = new();
    }

    public class GroupAttributeAssignmentDto
    {
        public long ProductAttributeId { get; set; }
        public bool IsRequired { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
        public string? DefaultValue { get; set; }
    }

    public class ProductAttributeDto
    {
        public long ProductAttributeId { get; set; }
        public string AttributeCode { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string DataType { get; set; } = "TEXT";
        public string? UnitLabel { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public List<ProductAttributeOptionDto> Options { get; set; } = new();
    }

    public class ProductAttributeOptionDto
    {
        public long ProductAttributeOptionId { get; set; }
        public long ProductAttributeId { get; set; }
        public string OptionCode { get; set; } = string.Empty;
        public string OptionValue { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ProductGroupFilterDto
    {
        public string? Keyword { get; set; }
        public string? Status { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
