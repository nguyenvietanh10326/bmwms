using BMWMS.Business.Common;

namespace BMWMS.Business.DTOs.Report;

public class StocktakeStatisticsResponseDto
{
    public PagedResultDto<StocktakeStatisticsItemDto> Data { get; set; } = new();
    public DateTime CutOffTime { get; set; } = DateTime.UtcNow;
}
