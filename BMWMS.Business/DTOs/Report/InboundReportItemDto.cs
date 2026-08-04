using System;

namespace BMWMS.Business.DTOs.Report
{
    public class InboundReportItemDto
    {
        public string InboundOrderNumber { get; set; } = null!;
        public string ProductCode { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string SourceType { get; set; } = null!;
        public DateOnly ExpectedReceiptDate { get; set; }
        public string Status { get; set; } = null!;
        public decimal ExpectedQuantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal DamagedQuantity { get; set; }
        public decimal ShortageQuantity { get; set; }
    }
}
