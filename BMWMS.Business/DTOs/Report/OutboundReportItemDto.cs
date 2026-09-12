using System;

namespace BMWMS.Business.DTOs.Report
{
    public class OutboundReportItemDto
    {
        public string OutboundOrderNumber { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitCode { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public DateTime ExpectedIssueDate { get; set; }
        public decimal RequestedQuantity { get; set; }
        public decimal IssuedQuantity { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
