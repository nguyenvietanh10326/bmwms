using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BMWMS.Web.Models
{
    public class ProductFilterModel
    {
        public string? Keyword { get; set; }
        public long? ProductGroupId { get; set; }
        public int? UnitOfMeasureId { get; set; }
        public string? Status { get; set; }
        public string? RotationMethod { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class ProductAttributeValueModel
    {
        public long ProductAttributeId { get; set; }
        public string? AttributeCode { get; set; }
        public string? AttributeName { get; set; }
        public string? DataType { get; set; }
        public string? UnitLabel { get; set; }
        public string AttributeValue { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    public class ProductResponseModel
    {
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public long ProductGroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int UnitOfMeasureId { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public byte QuantityScale { get; set; }
        public string? Barcode { get; set; }
        public string? Description { get; set; }
        public string RotationMethod { get; set; } = "FIFO";
        public bool TrackLot { get; set; }
        public bool TrackExpiry { get; set; }
        public int? DefaultShelfLifeDays { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public decimal TotalStockOnHand { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ProductDetailResponseModel : ProductResponseModel
    {
        public string? CreatedByName { get; set; }
        public string? UpdatedByName { get; set; }
        public List<ProductAttributeValueModel> AttributeValues { get; set; } = new();
        public List<ProductStockLocationModel> StockLocations { get; set; } = new();
        public List<ProductWarehousePolicyModel> WarehousePolicies { get; set; } = new();
        public List<ProductSupplierModel> Suppliers { get; set; } = new();
    }

    public class ProductStockLocationModel
    {
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string? StorageLocationCode { get; set; }
        public decimal QuantityOnHand { get; set; }
        public decimal QuantityReserved { get; set; }
        public decimal QuantityAvailable => QuantityOnHand - QuantityReserved;
    }

    public class ProductWarehousePolicyModel
    {
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public decimal MinimumStockQuantity { get; set; }
        public int ExpiryWarningDays { get; set; }
    }

    public class ProductSupplierModel
    {
        public long SupplierId { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string? SupplierProductCode { get; set; }
        public int? LeadTimeDays { get; set; }
    }

    public class CreateProductModel
    {
        [Required(ErrorMessage = "Mã sản phẩm không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã sản phẩm tối đa 50 ký tự.")]
        [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Mã sản phẩm chỉ gồm chữ cái, số, gạch ngang hoặc gạch dưới.")]
        public string ProductCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
        [StringLength(200, ErrorMessage = "Tên sản phẩm tối đa 200 ký tự.")]
        public string ProductName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn nhóm danh mục.")]
        public long ProductGroupId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn đơn vị tính.")]
        public int UnitOfMeasureId { get; set; }

        [StringLength(50, ErrorMessage = "Mã vạch tối đa 50 ký tự.")]
        public string? Barcode { get; set; }

        [StringLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương pháp xuất kho.")]
        [RegularExpression(@"^(FIFO|FEFO)$", ErrorMessage = "Phương pháp xuất kho phải là FIFO hoặc FEFO.")]
        public string RotationMethod { get; set; } = "FIFO";

        public bool TrackExpiry { get; set; } = false;

        [Range(1, 3650, ErrorMessage = "Hạn sử dụng mặc định từ 1 đến 3650 ngày.")]
        public int? DefaultShelfLifeDays { get; set; }

        public string Status { get; set; } = "ACTIVE";

        public List<ProductAttributeValueModel>? AttributeValues { get; set; }
    }

    public class UpdateProductModel : CreateProductModel
    {
        [Required(ErrorMessage = "Trạng thái không được để trống.")]
        [RegularExpression(@"^(ACTIVE|INACTIVE)$", ErrorMessage = "Trạng thái chỉ nhận giá trị ACTIVE hoặc INACTIVE.")]
        public new string Status { get; set; } = "ACTIVE";
    }

    public class UnitOfMeasureModel
    {
        public int UnitOfMeasureId { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public byte QuantityScale { get; set; }
        public string UnitName { get; set; } = string.Empty;
    }

    public class ProductGroupOptionModel
    {
        public long ProductGroupId { get; set; }
        public string GroupCode { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
    }
}
