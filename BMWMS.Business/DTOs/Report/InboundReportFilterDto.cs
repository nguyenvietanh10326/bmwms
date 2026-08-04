using System;

namespace BMWMS.Business.DTOs.Report
{
    public class InboundReportFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? ProductSearch { get; set; }
        public string? Status { get; set; }
    }
}
