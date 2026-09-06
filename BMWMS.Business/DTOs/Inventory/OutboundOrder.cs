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
        public long? PurchaseOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public string SourceReference { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public long WarehouseId { get; set; }
        public string WarehouseName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int LineCount { get; set; }
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
        public long? PurchaseOrderId { get; set; }
        public string? SalesOrderNumber { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public string SourceReference { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
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
        public byte QuantityScale { get; set; }
        public bool TrackLot { get; set; }
        public decimal RequestedQuantity { get; set; }
        public decimal IssuedQuantity { get; set; }
        public string? Notes { get; set; }
        public List<OutboundPickedDetailDto> PickedDetails { get; set; } = new();
    }

    // Request Request tạo mới lệnh xuất kho 
    public class CreateOutboundOrderRequest
    {
        public long WarehouseId { get; set; }
        public string SourceType { get; set; } = "SALES_ORDER"; // "SalesOrder" hoặc "Direct"
        public long? SalesOrderId { get; set; }
        public long? PurchaseOrderId { get; set; }
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
        public long? AssignedToUserId { get; set; }

        // Cấu hình phân trang
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class SalesOrderDetailApiResponse
    {
        public long SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public long? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public List<SalesOrderItemDto> Items { get; set; } = new();
    }
    public class UserSelectDto
    {
        public long UserId { get; set; }
        public string FullName { get; set; } = null!;
    }
    public class SalesOrderApiResponse
    {
        public long SalesOrderId { get; set; }
        public string SalesOrderNumber { get; set; } = string.Empty;

        public string? CustomerName { get; set; }
        public string? Status { get; set; }
    }
    public class PurchaseOrderReturnOptionDto
    {
        public long PurchaseOrderId { get; set; }
        public string PurchaseOrderNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
    }

    public class PurchaseOrderForReturnDto
    {
        public long PurchaseOrderId { get; set; }
        public string PurchaseOrderNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public List<PurchaseOrderReturnItemDto> Items { get; set; } = new();
    }

    public class PurchaseOrderReturnItemDto
    {
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public decimal ReceivedQuantity { get; set; }
        public decimal ReturnedQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }
        public byte QuantityScale { get; set; }
        public bool TrackLot { get; set; }
    }
    public class SalesOrderItemDto
    {
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }          // SL Yêu cầu
        public decimal ReservedQuantity { get; set; }  // SL Đã giữ tồn (Available Reserved)
        public string UnitName { get; set; } = string.Empty;
        public byte QuantityScale { get; set; }
        public bool TrackLot { get; set; }
        public string? LotBinInfo { get; set; }        // Thông tin Lot / Bin giữ tồn
    }
   
        // DTO Màn 2: Màn hình Thực thi Xuất kho (Process Picking)
        public class OutboundProcessViewDto
        {
            public long OutboundOrderId { get; set; }
            public string OutboundOrderNumber { get; set; } = null!;
            public string SourceType { get; set; } = null!;
            public string? SalesOrderNumber { get; set; }
            public string? PurchaseOrderNumber { get; set; }
            public string SourceReference { get; set; } = string.Empty;
            public string PartnerName { get; set; } = string.Empty;
            public string? CustomerName { get; set; }
            public long WarehouseId { get; set; }
            public string WarehouseName { get; set; } = null!;
            public long? AssignedToUserId { get; set; }
            public string Status { get; set; } = null!;
            public string? Notes { get; set; }
            public DateOnly ExpectedIssueDate { get; set; }

            public List<OutboundProcessItemDto> Items { get; set; } = new();
        }

        public class OutboundProcessItemDto
        {
            public long OutboundOrderItemId { get; set; }
            public long ProductId { get; set; }
            public string ProductCode { get; set; } = null!;
            public string ProductName { get; set; } = null!;
            public string UnitName { get; set; } = null!;
            public byte QuantityScale { get; set; }
            public bool TrackLot { get; set; }

            // Mối quan hệ so sánh [SL Yêu cầu] vs [SL Đã Pick]
            public decimal RequestedQuantity { get; set; }
            public decimal IssuedQuantity { get; set; }
            public decimal RemainingQuantity => RequestedQuantity - IssuedQuantity;

            // Lịch sử các lần pick thực tế trước đó
            public List<OutboundPickedDetailDto> PickedDetails { get; set; } = new();

            // Danh sách Vị trí + Lô khả dụng trong kho để User chọn pick
            public List<AvailableStockLocationDto> AvailableLocations { get; set; } = new();
        }

        public class OutboundPickedDetailDto
        {
            public long OutboundOrderDetailId { get; set; }
            public string LocationCode { get; set; } = null!;
            public string LotNumber { get; set; } = null!;
            public DateOnly FirstReceivedDate { get; set; }
            public DateOnly? ExpiryDate { get; set; }
            public decimal IssuedQuantity { get; set; }
            public string RecordedByUserName { get; set; } = null!;
            public DateTime RecordedAt { get; set; }
        }

        public class AvailableStockLocationDto
        {
            public long StorageLocationId { get; set; }
            public string LocationCode { get; set; } = null!;
            public string LocationPath { get; set; } = null!;
            public long ProductLotId { get; set; }
            public string LotNumber { get; set; } = null!;
            public DateOnly FirstReceivedDate { get; set; }
            public DateOnly? ExpiryDate { get; set; }
            public long? InventoryReservationId { get; set; }
            public decimal AvailableQuantity { get; set; } // Số lượng còn trong Bin/Lot
        }

        // Request Submit hành động Pick hàng từ Màn 2
        public class ExecutePickItemRequest
        {
            public long OutboundOrderId { get; set; }
            public long OutboundOrderItemId { get; set; }
            public long StorageLocationId { get; set; }
            public long ProductLotId { get; set; }
            public long? InventoryReservationId { get; set; }
            public decimal PickQuantity { get; set; }
            public string? Notes { get; set; }
        }
    
}
