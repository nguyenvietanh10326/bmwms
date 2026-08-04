using System;

namespace BMWMS.Business.DTOs.Report;

public class SupplierStatisticsItemDto
{
    public long SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;

    public int InboundOrderCount { get; set; }
    public decimal ExpectedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal DamagedQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }

    public decimal CompletionPerformance => ExpectedQuantity > 0 ? (ReceivedQuantity / ExpectedQuantity) * 100 : 0;
}
