using System.Collections.Generic;

namespace BMWMS.Business.DTOs.Report
{
    public class InboundReportResponseDto
    {
        public decimal TotalExpected { get; set; }
        public decimal TotalReceived { get; set; }
        public decimal TotalDamaged { get; set; }
        public decimal TotalShortage { get; set; }
        public List<InboundReportItemDto> Items { get; set; } = new List<InboundReportItemDto>();
    }
}
