using BMWMS.Business.Common;

namespace BMWMS.Business.DTOs.Report;

public class OverdueOrderAlertResponseDto
{
    public PagedResultDto<OverdueOrderAlertItemDto> Data { get; set; } = new();
    public DateTime CutOffTime { get; set; } = DateTime.UtcNow;
}
