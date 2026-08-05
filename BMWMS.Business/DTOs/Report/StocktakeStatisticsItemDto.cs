namespace BMWMS.Business.DTOs.Report;

public class StocktakeStatisticsItemDto
{
    public long StocktakeSessionId { get; set; }
    public string StocktakeNumber { get; set; } = string.Empty;
    public DateOnly PlannedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    
    public int BinsCounted { get; set; }
    public int MatchedItems { get; set; }
    public int ShortageItems { get; set; }
    public int ExcessItems { get; set; }
    public int TotalItemsCounted { get; set; }
    
    public decimal TotalShortageQuantity { get; set; }
    public decimal TotalExcessQuantity { get; set; }
    
    public decimal TotalApprovedAdjustmentQuantity { get; set; }
    
    public double StockAccuracyRate => TotalItemsCounted > 0 ? (double)MatchedItems / TotalItemsCounted * 100.0 : 0;
}
