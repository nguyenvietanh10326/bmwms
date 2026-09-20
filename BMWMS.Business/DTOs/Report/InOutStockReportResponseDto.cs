using System.Collections.Generic;

namespace BMWMS.Business.DTOs.Report
{
    public class InOutStockReportResponseDto
    {
        public int TotalCount { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public DateTime? CutOffTime { get; set; }
        public List<InOutStockReportItemDto> Items { get; set; } = new();
    }
}
