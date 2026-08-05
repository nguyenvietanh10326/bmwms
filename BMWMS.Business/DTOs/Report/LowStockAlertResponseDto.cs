using BMWMS.Business.Common;

namespace BMWMS.Business.DTOs.Report;

public class LowStockAlertResponseDto
{
    public PagedResultDto<LowStockAlertItemDto> Data { get; set; } = new();
    public DateTime CutOffTime { get; set; } = DateTime.UtcNow;
}
