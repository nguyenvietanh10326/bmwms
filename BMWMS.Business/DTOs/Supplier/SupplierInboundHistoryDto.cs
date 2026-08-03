using System;
using System.Collections.Generic;

namespace BMWMS.Business.DTOs.Supplier;

public class SupplierInboundHistoryFilterDto
{
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public long? WarehouseId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SupplierInboundHistoryResponseDto
{
    public string InboundOrderNumber { get; set; } = null!;
    public string? PurchaseOrderNumber { get; set; }
    public string SupplierName { get; set; } = null!;
    public string? WarehouseName { get; set; }
    public string Status { get; set; } = null!;
    public int TotalQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SupplierInboundHistoryDetailDto
{
    public string InboundOrderNumber { get; set; } = null!;
    public string? PurchaseOrderNumber { get; set; }
    public string SupplierName { get; set; } = null!;
    public string? WarehouseName { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpectedDate { get; set; }
    public DateTime? ReceiptDate { get; set; }
    
    public List<SupplierInboundHistoryItemDto> Items { get; set; } = new();
}

public class SupplierInboundHistoryItemDto
{
    public string ProductCode { get; set; } = null!;
    public string ProductName { get; set; } = null!;
    public string UnitName { get; set; } = null!;
    public int ExpectedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
}
