using System.Collections.Generic;

namespace BMWMS.Business.DTOs.ProductAttribute;

public class ProductAttributeDto
{
    public long ProductAttributeId { get; set; }
    public string AttributeCode { get; set; } = null!;
    public string AttributeName { get; set; } = null!;
    public string DataType { get; set; } = null!;
    public string? UnitLabel { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = null!;
    public List<ProductAttributeOptionDto> Options { get; set; } = new();
}

public class CreateProductAttributeDto
{
    public string AttributeCode { get; set; } = null!;
    public string AttributeName { get; set; } = null!;
    public string DataType { get; set; } = null!;
    public string? UnitLabel { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public List<ProductAttributeOptionDto> Options { get; set; } = new();
}

public class UpdateProductAttributeDto
{
    public string AttributeCode { get; set; } = null!;
    public string AttributeName { get; set; } = null!;
    public string DataType { get; set; } = null!;
    public string? UnitLabel { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "ACTIVE";
    public List<ProductAttributeOptionDto> Options { get; set; } = new();
}

public class ProductAttributeOptionDto
{
    public long ProductAttributeOptionId { get; set; }
    public long ProductAttributeId { get; set; }
    public string OptionCode { get; set; } = null!;
    public string OptionValue { get; set; } = null!;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
