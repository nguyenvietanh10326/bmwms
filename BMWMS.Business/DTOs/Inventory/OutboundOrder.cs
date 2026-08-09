using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Business.DTOs.Inventory
{
    public class OutboundOrderListDto
    {
        public long OutboundOrderId { get; set; }
        public string OutboundOrderNumber { get; set; } = null!;
        public string SourceType { get; set; } = null!;
        public long? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public string? CustomerName { get; set; }
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public decimal TotalRequestedQuantity { get; set; }
        public decimal TotalIssuedQuantity { get; set; }
        public DateOnly ExpectedIssueDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // DTO xem chi tiết cho UC28 (Xem chi tiết lệnh xuất kho)
    public class OutboundOrderDetailDto
    {
        public long OutboundOrderId { get; set; }
        public string OutboundOrderNumber { get; set; } = null!;
        public string SourceType { get; set; } = null!;
        public long? SalesOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public string? CustomerName { get; set; }
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = null!;
        public DateOnly ExpectedIssueDate { get; set; }
        public long? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<OutboundOrderItemDto> Items { get; set; } = new();
    }

    public class OutboundOrderItemDto
    {
        public long OutboundOrderItemId { get; set; }
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string UnitOfMeasure { get; set; } = null!;
        public decimal RequestedQuantity { get; set; }
        public decimal IssuedQuantity { get; set; }
        public string? Notes { get; set; }
    }

    // Request Request tạo mới lệnh xuất kho 
    public class CreateOutboundOrderRequest
    {
        public long WarehouseId { get; set; }
        public string SourceType { get; set; } = "SalesOrder"; // "SalesOrder" hoặc "Direct"
        public long? SalesOrderId { get; set; }
        public DateOnly ExpectedIssueDate { get; set; }
        public long? AssignedToUserId { get; set; }
        public string? Notes { get; set; }
        public bool IsSubmit { get; set; } = false; // true: Chờ xử lý, false: Nháp

        public List<CreateOutboundOrderItemRequest> Items { get; set; } = new();
    }

    public class CreateOutboundOrderItemRequest
    {
        public long ProductId { get; set; }
        public decimal RequestedQuantity { get; set; }
        public string? Notes { get; set; }
    }

    // Request Cập nhật trạng thái lệnh xuất kho
    public class UpdateOutboundOrderStatusRequest
    {
        public long OutboundOrderId { get; set; }
        public string Status { get; set; } = null!; // "Nháp", "Chờ xử lý", "Đang picking", "Đã xuất", "Đã hủy"
    }
    // Request chứa các tham số lọc + phân trang
    public class OutboundOrderQueryFilter
    {
        public string? Search { get; set; }
        public string? Status { get; set; }
        public long? WarehouseId { get; set; }

        // Cấu hình phân trang
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    
}
