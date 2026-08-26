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

        public int ItemCount { get; set; }
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
        public string? SupplierEmail { get; set; }

        public DateOnly OrderDate { get; set; }
        public DateOnly? ExpectedDeliveryDate { get; set; }

        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }

        public long CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public long? ConfirmedByUserId { get; set; }
        public string? ConfirmedByUserName { get; set; }
        public DateTime? ConfirmedAt { get; set; }

        public long? SupplierEmailSentByUserId { get; set; }
        public DateTime? SupplierEmailSentAt { get; set; }
        public string? SupplierEmailSentTo { get; set; }
        public bool HasSentSupplierEmail => SupplierEmailSentAt.HasValue;

        public bool CanSendToSupplier { get; set; }
        public bool CanConfirm { get; set; }
        public bool CanCancel { get; set; }
        public bool CanCreateInbound { get; set; }

        public List<PurchaseOrderItemDto> Items { get; set; } = new();
        public List<RelatedInboundDto> Inbounds { get; set; } = new();
    }

    public class RelatedInboundDto
    {
        public long InboundOrderId { get; set; }
        public string InboundOrderNumber { get; set; } = string.Empty;
        public DateOnly ExpectedReceiptDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal ExpectedQuantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public bool IsSupplemental { get; set; }
    }

    public class PurchaseOrderItemDto
    {
        public long PurchaseOrderDetailId { get; set; }
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal OrderedQuantity { get; set; }
        public decimal PlannedInboundQuantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }
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

    public class CustomerLookupDto
    {
        public long CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string DisplayName => $"{CustomerCode} — {CustomerName}";
    }
    public class ProductLookupDto
    {
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitOfMeasure { get; set; } = string.Empty;
        public decimal PurchasePrice { get; set; }
        public decimal OnHandQuantity { get; set; }

        public decimal ReservedQuantity { get; set; }

        public decimal? AvailableQuantity { get; set; }
        public long ProductGroupId { get; set; }
        public string ProductGroupName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string RotationMethod { get; set; } = "FIFO";
        public string DisplayName => $"{ProductCode} — {ProductName}";
    }
}
