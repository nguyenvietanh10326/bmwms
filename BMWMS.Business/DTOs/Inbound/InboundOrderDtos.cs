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
