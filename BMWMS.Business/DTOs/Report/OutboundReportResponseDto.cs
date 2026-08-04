using System.Collections.Generic;

namespace BMWMS.Business.DTOs.Report
{
    public class OutboundReportResponseDto
    {
        public decimal TotalRequested { get; set; }
        public decimal TotalIssued { get; set; }
        
        public int TotalCount { get; set; }
        public List<OutboundReportItemDto> Items { get; set; } = new();
    }
}
