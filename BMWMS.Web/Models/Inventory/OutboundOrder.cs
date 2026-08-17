using BMWMS.Web.Models.Warehouse;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMWMS.Web.Models.Inventory
{
    public class OutboundOrderListDto
    {
        public long OutboundOrderId { get; set; }
        public string OutboundOrderNumber { get; set; } = null!;
        public string SourceType { get; set; } = null!;
        public long? SalesOrderId { get; set; }
        public long? PurchaseOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public string SourceReference { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
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
        public DateTime ExpectedIssueDate { get; set; }
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
        // Cấu hình phân trang
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
    public class OutboundOrderIndexViewModel
    {
        public PagedResultDto<OutboundOrderListDto> PagedResult { get; set; } = new();

        public OutboundOrderQueryFilter Filter { get; set; } = new();

        public List<SelectListItem> StatusOptions { get; set; } = new()
        {
            new SelectListItem { Text = "-- Tất cả trạng thái --", Value = "" },
            new SelectListItem { Text = "Nháp (DRAFT)", Value = "DRAFT" },
            new SelectListItem { Text = "Đã phân công (ASSIGNED)", Value = "ASSIGNED" },
            new SelectListItem { Text = "Đang xử lý (IN_PROGRESS)", Value = "IN_PROGRESS" },
            new SelectListItem { Text = "Hoàn thành (COMPLETED)", Value = "COMPLETED" },
            new SelectListItem { Text = "Đã hủy (CANCELLED)", Value = "CANCELLED" }
        };

        public List<SelectListItem> WarehouseOptions { get; set; } = new();
    }
    public class WarehouseDto
    {
        public int WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
    }

    public class WarehouseApiResponse
    {
        public long WarehouseId { get; set; }
        public string WarehouseCode { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
    }

    public class SalesOrderApiResponse
    {
        public long SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
    }

    public class SalesOrderDetailApiResponse
    {
        public long SalesOrderItemId { get; set; }
        public long SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public long? WarehouseId { get; set; }
        public List<SalesOrderItemApiResponse> Items { get; set; } = new();
    }

    public class SalesOrderItemApiResponse
    {
        public long SalesOrderItemId { get; set; }
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string? LotBinInfo { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class UserApiResponse
    {
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FullName { get; set; }
    }
    // View Model Classes
    public class CreateOutboundOrderInput
    {
        public long SalesOrderId { get; set; }
        public string? OrderNumberAuto { get; set; } = "Tự động";
        public string? CustomerName { get; set; }
        public long WarehouseId { get; set; }
        public DateTime ExpectedIssueDate { get; set; }
        public long? AssignedToUserId { get; set; }
        public string PickingStrategy { get; set; } = "FEFO";
        public string? ReferenceCode { get; set; }
        public string? Notes { get; set; }

        public List<CreateItemRowInput> Items { get; set; } = new();
    }

    public class CreateItemRowInput

    {
        public long SalesOrderItemId { get; set; }
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal RequestedQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public string UnitName { get; set; } = string.Empty;
        public string LotBin { get; set; } = string.Empty;
        public string CheckResult { get; set; } = string.Empty;
        public bool IsEnough { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
    }

}
