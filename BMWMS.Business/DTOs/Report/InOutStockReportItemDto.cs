namespace BMWMS.Business.DTOs.Report
{
    public class InOutStockReportItemDto
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal InboundQuantity { get; set; }
        public decimal OutboundQuantity { get; set; }
        public decimal AdjustmentQuantity { get; set; }
        public decimal ClosingBalance { get; set; }
    }
}
