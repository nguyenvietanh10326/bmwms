using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Business.DTOs.Inventory
{
    #region 1. DTO Cho Màn Hình Danh Sách (Grid View)
    public class PurchaseOrderListDto
    {
        public long PurchaseOrderId { get; set; }
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        // Thông tin Nhà cung cấp
        public long SupplierId { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string SupplierDisplayText => $"{SupplierCode} — {SupplierName}";

        public DateOnly OrderDate { get; set; }
        public DateOnly? ExpectedDeliveryDate { get; set; }
        public string Status { get; set; } = string.Empty;

        // Tổng số lượng & Đơn vị tính đại diện (Dùng hiển thị cột "Tổng SL")
        public decimal TotalQuantity { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string DisplayTotalQuantity => $"{TotalQuantity:N0} {UnitName}".Trim();
    }
    #endregion

    #region 2. DTO Cho Màn Hình Chi Tiết (Detail View)
    public class PurchaseOrderDetailDto
    {
        public long PurchaseOrderId { get; set; }
        public string PurchaseOrderNumber { get; set; } = string.Empty;

        public long SupplierId { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;

        public DateOnly OrderDate { get; set; }
        public DateOnly? ExpectedDeliveryDate { get; set; }
        public long? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }

        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }

        public long CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public long? ConfirmedByUserId { get; set; }
        public string? ConfirmedByUserName { get; set; }
        public DateTime? ConfirmedAt { get; set; }

        public decimal TotalAmount { get; set; }

        public List<PurchaseOrderItemDto> Items { get; set; } = new();
    }

    public class PurchaseOrderItemDto
    {
        public long PurchaseOrderDetailId { get; set; }
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal OrderedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => OrderedQuantity * UnitPrice;
        public string? Notes { get; set; }
    }
    #endregion

    #region 3. DTO Cho Màn Hình Tạo Mới / Sửa (Create/Update Request)
    public class CreatePurchaseOrderDto
    {
        [Required(ErrorMessage = "Vui lòng chọn Nhà cung cấp")]
        public long SupplierId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Ngày đặt hàng")]
        public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        public DateOnly? ExpectedDeliveryDate { get; set; }

        public long? WarehouseId { get; set; } // Kho dự kiến nhận hàng

        public string? Notes { get; set; }

        // Bỏ trống hoặc "Draft" nếu bấm "Lưu nháp", "Confirmed" nếu bấm "Xác nhận đơn mua"
        public bool IsSubmitForConfirmation { get; set; } = false;

        [MinLength(1, ErrorMessage = "Đơn mua hàng phải có ít nhất 1 sản phẩm")]
        public List<CreatePurchaseOrderItemDto> Items { get; set; } = new();
    }

    public class CreatePurchaseOrderItemDto
    {
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        public long ProductId { get; set; }

        [Range(0.0001, double.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public decimal OrderedQuantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không hợp lệ")]
        public decimal UnitPrice { get; set; }

        public string? Notes { get; set; }
    }
    #endregion

    #region 4. DTO Bộ Lọc & Phân Trang (Filter Query)
    public class PurchaseOrderFilterDto
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public long? WarehouseId { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PagedResultDto<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
    #endregion

    public class WarehouseLookupDto
    {
        public long WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
    }
    public class SupplierLookupDto
    {
        public long SupplierId { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string DisplayName => $"{SupplierCode} — {SupplierName}";
    }
    public class ProductLookupDto
    {
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        public string DisplayName => $"{ProductCode} — {ProductName}";
    }
}
