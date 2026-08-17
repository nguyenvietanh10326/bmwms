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
        public string SourceLocationSummary { get; set; } = string.Empty;
        public string DestinationLocationSummary { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusCss { get; set; } = string.Empty;
        public string ProgressLabel { get; set; } = string.Empty;
        public string NextAction { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
        public string? AssignedToName { get; set; }
        public string? ConfirmedByName { get; set; }
        public DateOnly RequestedDate { get; set; }
        public DateOnly? DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public int TotalItems { get; set; }
        public decimal TotalRequestedQuantity { get; set; }
        public decimal TotalMovedQuantity { get; set; }
        public bool InventoryPosted { get; set; }
        public bool IsWorkflowLinked { get; set; }
        public string? ParentDocumentType { get; set; }
        public long? ParentDocumentId { get; set; }
        public string? ParentDocumentNumber { get; set; }
        public string? Notes { get; set; }
    }

    public class TransferOrderPagedResultDto
    {
        public List<TransferOrderListDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
        public int DraftCount { get; set; }
        public int ApprovedCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        // backward-compat aliases
        public int PendingCount => DraftCount;
        public int RejectedCount => CancelledCount;
        public int IssuedCount => InProgressCount;
        public int ReceivedCount => CompletedCount;
    }

    // ─── DETAIL VIEW ────────────────────────────────────────────────────────

    public class TransferOrderDetailViewDto
    {
        public long TransferOrderId { get; set; }
        public string TransferOrderNumber { get; set; } = string.Empty;
        public string TransferType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public string ProgressLabel { get; set; } = string.Empty;
        public string NextAction { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public DateOnly RequestedDate { get; set; }
        public DateOnly? DueDate { get; set; }
        public string? Notes { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? AssignedToName { get; set; }
        public long? AssignedToUserId { get; set; }
        public string? ConfirmedByName { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public bool InventoryPosted { get; set; }
        public bool CanEdit { get; set; }
        public bool IsWorkflowLinked { get; set; }
        public string? ParentDocumentType { get; set; }
        public long? ParentDocumentId { get; set; }
        public string? ParentDocumentNumber { get; set; }
        public bool CanApprove { get; set; }
        public bool CanReject { get; set; }
        public bool CanIssue { get; set; }
        public bool CanReceive { get; set; }
        public decimal TotalRequestedQuantity { get; set; }
        public decimal TotalMovedQuantity { get; set; }
        public List<TransferOrderDetailItemDto> Details { get; set; } = new();
    }

    public class TransferOrderDetailItemDto
    {
        public long TransferOrderDetailId { get; set; }
        public long ProductId { get; set; }
        public long ProductLotId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public string LotNumber { get; set; } = string.Empty;
        // Source hierarchy
        public long SourceLocationId { get; set; }
        public long? SourceZoneId { get; set; }
        public long? SourceRackId { get; set; }
        public string SourceZoneCode { get; set; } = string.Empty;
        public string SourceRackCode { get; set; } = string.Empty;
        public string SourceLocationCode { get; set; } = string.Empty;
        // Dest hierarchy
        public long DestLocationId { get; set; }
        public long? DestZoneId { get; set; }
        public long? DestRackId { get; set; }
        public string DestZoneCode { get; set; } = string.Empty;
        public string DestRackCode { get; set; } = string.Empty;
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

    /// <summary>Manager tạo lệnh chuyển kho — hỗ trợ nhiều sản phẩm trong 1 lần chuyển</summary>
    public class CreateTransferOrderDto
    {
        public long WarehouseId { get; set; } = 1;
        public long? AssignedToUserId { get; set; }   // Manager assign Staff
        public DateOnly? DueDate { get; set; }         // Ngày dự kiến hoàn thành
        public string? Notes { get; set; }
        public List<CreateTransferItemDto> Items { get; set; } = new();
    }

    public class UpdateTransferOrderDto : CreateTransferOrderDto
    {
        public long TransferOrderId { get; set; }
    }

    public class ApproveTransferDto
    {
        public long TransferOrderId { get; set; }
        public long? AssignedToUserId { get; set; }   // Có thể assign/re-assign lúc duyệt
        public string? Notes { get; set; }
    }

    public class ConfirmTransferDto
    {
        public string? Notes { get; set; }
    }

    public class TransferUseCaseDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public string Preconditions { get; set; } = string.Empty;
        public string MainFlow { get; set; } = string.Empty;
        public string ResultStatus { get; set; } = string.Empty;
        public string ApiEndpoint { get; set; } = string.Empty;
    }

    // ─── DROPDOWN & HELPER DTOs ─────────────────────────────────────────────

    public class ZoneOptionDto
    {
        public long ZoneId { get; set; }
        public string ZoneCode { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
    }

    public class RackOptionDto
    {
        public long RackId { get; set; }
        public string RackCode { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
        public long ZoneId { get; set; }
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
        public long? RackId { get; set; }
        public string RackCode { get; set; } = string.Empty;
        public bool IsPutawayAllowed { get; set; }
        public bool IsPickable { get; set; }
        public string Status { get; set; } = string.Empty;
        public string DisplayLabel => string.IsNullOrEmpty(ZoneCode) ? LocationCode : $"{ZoneCode} / {RackCode} / {LocationCode}";
    }

    public class BinCapacityCheckDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class StaffOptionDto
    {
        public long UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string RoleCode { get; set; } = string.Empty;
    }

    public class TransferResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public long? TransferOrderId { get; set; }
        public string? TransferOrderNumber { get; set; }
    }
}
