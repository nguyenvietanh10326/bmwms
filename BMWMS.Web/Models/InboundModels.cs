using System;
using System.Collections.Generic;

namespace BMWMS.Web.Models;

public class InboundOrderFilterModel
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public string? SourceType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public long? AssignedToUserId { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class AvailableWarehouseStaffDto
{
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
}

public class InboundOrderListModel
{
    public long InboundOrderId { get; set; }
    public string InboundOrderNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public DateOnly ExpectedReceiptDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalExpectedQuantity { get; set; }
    public decimal TotalReceivedQuantity { get; set; }
    public int LineCount { get; set; }
    public bool IsSupplemental { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InboundOrderPageModel
{
    public int TotalCount { get; set; }
    public List<InboundOrderListModel> Items { get; set; } = new();
}

public class InboundOrderDetailDto
{
    public long InboundOrderId { get; set; }
    public string InboundOrderNumber { get; set; } = string.Empty;
    public string? PurchaseOrderNumber { get; set; }
    public long? PurchaseOrderId { get; set; }
    public string? SalesOrderNumber { get; set; }
    public long? SalesOrderId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateOnly ExpectedReceiptDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public long? AssignedToUserId { get; set; }
    public string AssignedToUserName { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public string? ParentInboundOrderNumber { get; set; }
    public long? ParentInboundOrderId { get; set; }
    public List<InboundOrderItemDto> Items { get; set; } = new();
    public List<InboundOrderTimelineDto> Timeline { get; set; } = new();
}

public class InboundOrderItemDto
{
    public long InboundOrderItemId { get; set; }
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public byte QuantityScale { get; set; }
    public bool TrackLot { get; set; }
    public bool TrackExpiry { get; set; }
    public string RotationMethod { get; set; } = string.Empty;
    public decimal ExpectedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal PutawayQuantity { get; set; }
    public decimal DamagedQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }
    public decimal SupplementalRemainingQuantity { get; set; }
    public string? Notes { get; set; }
    public string? LotNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public DateOnly? ManufactureDate { get; set; }
    public List<InboundReceiptDto> Receipts { get; set; } = new();
}

public class InboundReceiptDto
{
    public long InboundOrderDetailId { get; set; }
    public long ProductLotId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public DateOnly? ManufactureDate { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal PutawayQuantity { get; set; }
    public string ConditionStatus { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public string ZoneCode { get; set; } = string.Empty;
    public string RackCode { get; set; } = string.Empty;
    public string RecordedByUserName { get; set; } = string.Empty;
    public DateTime RecordedAt { get; set; }
    public string? Notes { get; set; }
    public string PutawayByUserName { get; set; } = string.Empty;
    public DateTime? PutawayAt { get; set; }
}

public class ReceiveInboundItemDto
{
    public long InboundOrderItemId { get; set; }
    public decimal? DeliveredQuantity { get; set; }
    public decimal? AcceptedQuantity { get; set; }
    public decimal? RejectedQuantity { get; set; }
    public string? LotNumber { get; set; }
    public DateOnly? ManufactureDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? ConditionNotes { get; set; }
}

public class ReceiveBatchInboundDto
{
    public List<ReceiveInboundItemDto> Items { get; set; } = new();
}

public class CompleteInboundReceiptDto
{
    public string? Notes { get; set; }
    public List<InboundReceiptDecisionDto> Decisions { get; set; } = new();
}

public class InboundReceiptDecisionDto
{
    public long InboundOrderItemId { get; set; }
    public bool CloseAsShort { get; set; }
    public string? Reason { get; set; }
}

public class PutawayInboundItemDto
{
    public long InboundOrderItemId { get; set; }
    public long ProductLotId { get; set; }
    public long StorageLocationId { get; set; }
    public decimal PutawayQuantity { get; set; }
}

public class InboundOrderTimelineDto
{
    public DateTime EventTime { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string StatusBadge { get; set; } = string.Empty;
    public string StatusBadgeColor { get; set; } = string.Empty;
    public string EventDescription { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
}

public class SourceOrderDropdownDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class PurchaseOrderInboundSourceDto
{
    public long PurchaseOrderId { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public decimal ActivePlannedQuantity { get; set; }
    public decimal AvailableToPlanQuantity { get; set; }
    public int ProductLineCount { get; set; }
    public int PreviousReceiptCount { get; set; }
    public DateTime? LatestReceiptAt { get; set; }
    public bool IsFollowUpReceipt { get; set; }
    public string SearchText { get; set; } = string.Empty;
}

public class CreateInboundOrderDto
{
    public string SourceType { get; set; } = "PURCHASE_ORDER";
    public long? PurchaseOrderId { get; set; }
    public long? SalesOrderId { get; set; }
    public DateOnly ExpectedReceiptDate { get; set; }
    public string? Notes { get; set; }
    public long? AssignedToUserId { get; set; }
    public bool IsSubmit { get; set; }
    public List<CreateInboundOrderItemDto> Items { get; set; } = new();
}

public class CreateInboundOrderItemDto
{
    public long ProductId { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal ActualReceivedQuantity { get; set; }
    public DateOnly? ManufactureDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseOrderForInboundDto
{
    public long PurchaseOrderId { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public bool IsFollowUpReceipt { get; set; }
    public int PreviousReceiptCount { get; set; }
    public decimal TotalOrderedQuantity { get; set; }
    public decimal TotalAcceptedQuantity { get; set; }
    public decimal TotalRejectedQuantity { get; set; }
    public decimal TotalActivePlannedQuantity { get; set; }
    public decimal TotalAvailableToPlanQuantity { get; set; }
    public List<PurchaseOrderInboundHistoryDto> PreviousInbounds { get; set; } = new();
    public List<PurchaseOrderItemForInboundDto> Items { get; set; } = new();
}

public class PurchaseOrderInboundHistoryDto
{
    public long InboundOrderId { get; set; }
    public string InboundOrderNumber { get; set; } = string.Empty;
    public DateOnly ExpectedReceiptDate { get; set; }
    public DateTime? ActualReceiptAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AssignedToUserName { get; set; } = string.Empty;
    public decimal ExpectedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
}

public class PurchaseOrderItemForInboundDto
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public byte QuantityScale { get; set; }
    public bool TrackLot { get; set; }
    public bool TrackExpiry { get; set; }
    public string RotationMethod { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal InboundQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public decimal ActivePlannedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
}

public class UpdateInboundOrderDto
{
    public string? Notes { get; set; }
    public long? AssignedToUserId { get; set; }
    public bool IsSubmit { get; set; }
}

public class CancelInboundOrderDto
{
    public string? CancellationReason { get; set; }
}
