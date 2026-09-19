using System.Collections.Generic;

namespace BMWMS.Business.DTOs.Report
{
    public class OutboundReportResponseDto
    {
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public DateTime? CutOffTime { get; set; }
        public List<OutboundReportItemDto> Items { get; set; } = new();
    }
}
