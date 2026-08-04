using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BMWMS.Web.Models
{
    public class ProductGroupViewModel
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

    public class ProductGroupDetailViewModel : ProductGroupViewModel
    {
        public List<GroupAttributeConfigViewModel> Attributes { get; set; } = new();
    }

    public class CreateProductGroupInputModel
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
    }

    public class UpdateProductGroupInputModel
    {
        [Required(ErrorMessage = "Tên nhóm sản phẩm không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên nhóm tối đa 200 ký tự.")]
        public string GroupName { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [RegularExpression(@"^(ACTIVE|INACTIVE)$", ErrorMessage = "Trạng thái chỉ nhận giá trị ACTIVE hoặc INACTIVE.")]
        public string Status { get; set; } = "ACTIVE";
    }

    public class GroupAttributeConfigViewModel
    {
        public long ProductAttributeId { get; set; }
        public string AttributeCode { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string DataType { get; set; } = "TEXT";
        public string? UnitLabel { get; set; }
        public string? Description { get; set; }
        public bool IsRequired { get; set; }
        public int DisplayOrder { get; set; }
        public string? DefaultValue { get; set; }
        public List<ProductAttributeOptionViewModel> Options { get; set; } = new();
    }

    public class ProductAttributeOptionViewModel
    {
        public long ProductAttributeOptionId { get; set; }
        public long ProductAttributeId { get; set; }
        public string OptionCode { get; set; } = string.Empty;
        public string OptionValue { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ProductAttributeViewModel
    {
        public long ProductAttributeId { get; set; }
        public string AttributeCode { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string DataType { get; set; } = "TEXT";
        public string? UnitLabel { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public List<ProductAttributeOptionViewModel> Options { get; set; } = new();
    }

    public class GroupAttributeAssignmentInputModel
    {
        public long ProductAttributeId { get; set; }
        public bool IsRequired { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
        public string? DefaultValue { get; set; }
    }
}
