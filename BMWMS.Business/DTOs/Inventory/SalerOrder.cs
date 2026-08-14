using System;
using System.Collections.Generic;
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
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
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
        public decimal TotalQuantity { get; set; }
        public string PrimaryUnitName { get; set; } = string.Empty;
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
        public decimal OrderedQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal FulfilledQuantity { get; set; }
        public decimal AvailableQuantity { get; set; } // Tính từ On Hand - Reserved
        public decimal? UnitPrice { get; set; }
        public decimal TotalAmount => (UnitPrice ?? 0) * OrderedQuantity;
        public string? Notes { get; set; }
    }

    // DTO Tạo mới / Cập nhật
    public class CreateUpdateSalesOrderDto
    {
        public long? SalesOrderId { get; set; }
        public long CustomerId { get; set; }
        public DateOnly OrderDate { get; set; }
        public DateOnly? ExpectedIssueDate { get; set; }
        public string? Notes { get; set; }
        public long CurrentUserId { get; set; }
        public List<CreateUpdateSalesOrderItemDto> Items { get; set; } = new();
    }

    public class CreateUpdateSalesOrderItemDto
    {
        public long ProductId { get; set; }
        public decimal OrderedQuantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Notes { get; set; }
    }
}
