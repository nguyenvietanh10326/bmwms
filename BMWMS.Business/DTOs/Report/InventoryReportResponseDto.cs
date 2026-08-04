using BMWMS.Business.Common;

namespace BMWMS.Business.DTOs.Report;

public class InventoryReportResponseDto
{
    public PagedResultDto<InventoryReportItemDto> Data { get; set; } = new();
    
    public decimal TotalOnHand { get; set; }
    public decimal TotalReserved { get; set; }
    public decimal TotalAvailable { get; set; }
    
    public DateTime CutOffTime { get; set; } = DateTime.UtcNow;
}
