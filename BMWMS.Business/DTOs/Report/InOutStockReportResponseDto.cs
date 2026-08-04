using System.Collections.Generic;

namespace BMWMS.Business.DTOs.Report
{
    public class InOutStockReportResponseDto
    {
        public int TotalCount { get; set; }
        public List<InOutStockReportItemDto> Items { get; set; } = new();
    }
}
