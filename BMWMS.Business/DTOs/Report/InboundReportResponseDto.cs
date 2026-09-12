using System.Collections.Generic;

namespace BMWMS.Business.DTOs.Report
{
    public class InboundReportResponseDto
    {
        public int TotalCount { get; set; }
        
        public List<InboundReportItemDto> Items { get; set; } = new List<InboundReportItemDto>();
    }
}
