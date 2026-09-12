using BMWMS.Business.Common;

namespace BMWMS.Business.DTOs.Report;

public class InventoryReportResponseDto
{
    public PagedResultDto<InventoryReportItemDto> Data { get; set; } = new();
    
    public DateTime CutOffTime { get; set; } = DateTime.UtcNow;
}
