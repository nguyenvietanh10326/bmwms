using BMWMS.Business.Common;

namespace BMWMS.Business.DTOs.Report;

public class ExpiringLotAlertResponseDto
{
    public PagedResultDto<ExpiringLotAlertItemDto> Data { get; set; } = new();
    public DateTime CutOffTime { get; set; } = DateTime.UtcNow;
}
