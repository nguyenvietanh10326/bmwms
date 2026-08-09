using System;
using System.Collections.Generic;

namespace BMWMS.Business.DTOs.StockOperations
{
    // ─── FILTER & PAGING ────────────────────────────────────────────────────

    public class TransferOrderFilterDto
    {
        public string? Keyword { get; set; }
        public string? Status { get; set; }
        public long? WarehouseId { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }

    // ─── LIST VIEW ──────────────────────────────────────────────────────────

    public class TransferOrderListDto
    {
        public long TransferOrderId { get; set; }
        public string TransferOrderNumber { get; set; } = string.Empty;
        public string TransferType { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusCss { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public string? ConfirmedByName { get; set; }
        public DateOnly RequestedDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public int TotalItems { get; set; }
        public string? Notes { get; set; }
    }

    public class TransferOrderPagedResultDto
    {
        public List<TransferOrderListDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
    }

    // ─── DETAIL VIEW ────────────────────────────────────────────────────────

    public class TransferOrderDetailViewDto
    {
        public long TransferOrderId { get; set; }
        public string TransferOrderNumber { get; set; } = string.Empty;
        public string TransferType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public DateOnly RequestedDate { get; set; }
        public string? Notes { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? ConfirmedByName { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public List<TransferOrderDetailItemDto> Details { get; set; } = new();
    }

    public class TransferOrderDetailItemDto
    {
        public long TransferOrderDetailId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string LotNumber { get; set; } = string.Empty;
        public string SourceLocationCode { get; set; } = string.Empty;
        public string DestLocationCode { get; set; } = string.Empty;
        public decimal RequestedQuantity { get; set; }
        public decimal MovedQuantity { get; set; }
    }

    // ─── CREATE REQUEST (MULTI-ITEM) ────────────────────────────────────────

    public class CreateTransferItemDto
    {
        public long SourceLocationId { get; set; }
        public long DestLocationId { get; set; }
        public long ProductId { get; set; }
        public long ProductLotId { get; set; }
        public decimal Quantity { get; set; }
    }

    /// <summary>Staff tạo phiếu chuyển kho — hỗ trợ nhiều sản phẩm trong 1 lần chuyển</summary>
    public class CreateTransferOrderDto
    {
        public long WarehouseId { get; set; } = 1;
        public string? Notes { get; set; }
        public List<CreateTransferItemDto> Items { get; set; } = new();
    }

    public class ApproveTransferDto
    {
        public long TransferOrderId { get; set; }
        public string? Notes { get; set; }
    }

    // ─── DROPDOWN & HELPER DTOs ─────────────────────────────────────────────

    public class ZoneOptionDto
    {
        public long ZoneId { get; set; }
        public string ZoneCode { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
    }

    public class TransferInventoryItemDto
    {
        public long InventoryId { get; set; }
        public long ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public long ProductLotId { get; set; }
        public string LotNumber { get; set; } = string.Empty;
        public DateOnly? ExpiryDate { get; set; }
        public string ExpiryDisplay => ExpiryDate.HasValue ? ExpiryDate.Value.ToString("dd/MM/yyyy") : "—";
        public decimal OnHandQuantity { get; set; }
        public decimal ReservedQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
    }

    public class LocationOptionDto
    {
        public long LocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public long? ZoneId { get; set; }
        public string ZoneCode { get; set; } = string.Empty;
        public string RackCode { get; set; } = string.Empty;
        public bool IsPutawayAllowed { get; set; }
        public string Status { get; set; } = string.Empty;
        public string DisplayLabel => string.IsNullOrEmpty(ZoneCode) ? LocationCode : $"{ZoneCode}/{RackCode}/{LocationCode}";
    }

    public class BinCapacityCheckDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class TransferResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long? TransferOrderId { get; set; }
        public string? TransferOrderNumber { get; set; }
    }
}
