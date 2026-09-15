using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BMWMS.Web.Models
{
    public class ProductAttributeDto
    {
        public long ProductAttributeId { get; set; }
        public string AttributeCode { get; set; } = string.Empty;
        public string AttributeName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string? UnitLabel { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public List<ProductAttributeOptionDto> Options { get; set; } = new();
    }

    public class CreateProductAttributeDto
    {
        [Required(ErrorMessage = "Vui lòng nhập mã thuộc tính")]
        [RegularExpression(@"^[A-Z0-9_]+$", ErrorMessage = "Mã thuộc tính chỉ chứa chữ IN HOA, số và dấu gạch dưới")]
        public string AttributeCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên thuộc tính")]
        public string AttributeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn kiểu dữ liệu")]
        public string DataType { get; set; } = "TEXT";

        public string? UnitLabel { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = "ACTIVE";

        public List<ProductAttributeOptionDto> Options { get; set; } = new();
    }

    public class UpdateProductAttributeDto
    {
        [Required(ErrorMessage = "Vui lòng nhập mã thuộc tính")]
        [RegularExpression(@"^[A-Z0-9_]+$", ErrorMessage = "Mã thuộc tính chỉ chứa chữ IN HOA, số và dấu gạch dưới")]
        public string AttributeCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên thuộc tính")]
        public string AttributeName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn kiểu dữ liệu")]
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
}
