using System;

namespace BMWMS.Business.DTOs.Report;

public class ProductStatisticsItemDto
{
    public long ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string BaseUnitCode { get; set; } = string.Empty;

    public decimal CurrentStock { get; set; }
    public decimal InboundQuantity { get; set; }
    public decimal OutboundQuantity { get; set; }
    public decimal AdjustmentQuantity { get; set; }
    
    public int MovementFrequency { get; set; }
    public int DaysSinceLastMovement { get; set; }
}
