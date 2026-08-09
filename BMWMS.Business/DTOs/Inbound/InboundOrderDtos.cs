using System;
using System.Collections.Generic;

namespace BMWMS.Business.DTOs.Inbound;

public class InboundOrderFilterDto
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class InboundOrderListDto
{
    public long InboundOrderId { get; set; }
    public string InboundOrderNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateOnly ExpectedReceiptDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalExpectedQuantity { get; set; }
    public decimal TotalReceivedQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InboundOrderPageDto
{
    public int TotalCount { get; set; }
    public List<InboundOrderListDto> Items { get; set; } = new();
}

public class InboundOrderDetailDto
{
    public long InboundOrderId { get; set; }
    public string InboundOrderNumber { get; set; } = string.Empty;
    public string? PurchaseOrderNumber { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public DateOnly ExpectedReceiptDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AssignedToUserName { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;
    
    // Parent info for additional logic
    public string? ParentInboundOrderNumber { get; set; }
    
    public List<InboundOrderItemDto> Items { get; set; } = new();
    public List<InboundOrderTimelineDto> Timeline { get; set; } = new();
}

public class InboundOrderItemDto
{
    public long InboundOrderItemId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal ExpectedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal PutawayQuantity { get; set; }
    public string? LotNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}

public class InboundOrderTimelineDto
{
    public DateTime EventTime { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string StatusBadge { get; set; } = string.Empty;
    public string StatusBadgeColor { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
}

public class CreateInboundOrderDto
{
    public string SourceType { get; set; } = "PURCHASE_ORDER";
    public long? PurchaseOrderId { get; set; }
    public long? SalesOrderId { get; set; }
    public long WarehouseId { get; set; }
    public DateOnly ExpectedReceiptDate { get; set; }
    public string? Notes { get; set; }
    public long? ParentInboundOrderId { get; set; }
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng chọn người phụ trách")]
    public long? AssignedToUserId { get; set; }
    public List<CreateInboundOrderItemDto> Items { get; set; } = new();
}

public class ShortageInboundOrderDto
{
    public long InboundOrderId { get; set; }
    public string InboundOrderNumber { get; set; } = string.Empty;
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateOnly OriginalExpectedDate { get; set; }
}

public class CreateInboundOrderItemDto
{
    public long ProductId { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseOrderForInboundDto
{
    public long PurchaseOrderId { get; set; }
    public string PurchaseOrderNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public List<PurchaseOrderItemForInboundDto> Items { get; set; } = new();
}

public class PurchaseOrderItemForInboundDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal InboundQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
}
