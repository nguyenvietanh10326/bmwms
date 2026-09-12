using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Business.DTOs.Inventory
{
    // DTO Tìm kiếm & Phân trang
    public class SalesOrderSearchCriteria
    {
        public string? Keyword { get; set; }
        public string? Status { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // Result phân trang trả về cho UI
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize <= 0
            ? 0
            : (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    // DTO Danh sách hiển thị trang Index
    public class SalesOrderListDto
    {
        public long SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = null!;
        public string CustomerCode { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public DateOnly OrderDate { get; set; }
        public DateOnly? ExpectedIssueDate { get; set; }
        public string Status { get; set; } = null!;
        public int ItemCount { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // DTO Chi tiết Sales Order đầy đủ
    public class SalesOrderDetailDto
    {
        public long SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = null!;
        public long CustomerId { get; set; }
        public string CustomerCode { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public DateOnly OrderDate { get; set; }
        public DateOnly? ExpectedIssueDate { get; set; }
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
        public string AllocationStrategy { get; set; } = "FIFO";
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ConfirmedByName { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public List<SalesOrderItemDtos> Items { get; set; } = new();
    }

    public class SalesOrderItemDtos
    {
        public long SalesOrderDetailId { get; set; }
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string UnitName { get; set; } = string.Empty;
        public byte QuantityScale { get; set; }
        public bool TrackLot { get; set; }
        public decimal OrderedQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal FulfilledQuantity { get; set; }
        public decimal AvailableQuantity { get; set; } // Tính từ On Hand - Reserved
        public string? Notes { get; set; }
        public string AllocationStrategy { get; set; } = "FIFO";
    }

    // DTO Tạo mới / Cập nhật
    public class CreateUpdateSalesOrderDto
    {
        public long? SalesOrderId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Vui lòng chọn khách hàng.")]
        public long CustomerId { get; set; }

        public DateOnly OrderDate { get; set; }
        public DateOnly? ExpectedIssueDate { get; set; }

        [StringLength(2000, ErrorMessage = "Ghi chú không được vượt quá 2.000 ký tự.")]
        public string? Notes { get; set; }
        public string AllocationStrategy { get; set; } = "FIFO";
        public long CurrentUserId { get; set; }

        [MinLength(1, ErrorMessage = "Đơn bán hàng phải có ít nhất một sản phẩm.")]
        public List<CreateUpdateSalesOrderItemDto> Items { get; set; } = new();
    }

    public class CreateUpdateSalesOrderItemDto
    {
        [Range(1, long.MaxValue, ErrorMessage = "Sản phẩm không hợp lệ.")]
        public long ProductId { get; set; }

        [Range(typeof(decimal), "0.0001", "99999999999999", ErrorMessage = "Số lượng đặt phải lớn hơn 0.")]
        public decimal OrderedQuantity { get; set; }

        [StringLength(1000, ErrorMessage = "Ghi chú sản phẩm không được vượt quá 1.000 ký tự.")]
        public string? Notes { get; set; }
        public string AllocationStrategy { get; set; } = "FIFO";
    }

    public class SalesOrderProductLookupDto
    {
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public long ProductGroupId { get; set; }
        public string ProductGroupName { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string UnitOfMeasure { get; set; } = string.Empty;
        public byte QuantityScale { get; set; }
        public bool TrackLot { get; set; }
        public bool TrackExpiry { get; set; }
        public string RotationMethod { get; set; } = "FIFO";
        public decimal OnHandQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
    }
}
