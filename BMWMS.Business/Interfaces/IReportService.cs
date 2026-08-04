using BMWMS.Business.DTOs.Report;

namespace BMWMS.Business.Interfaces;

public interface IReportService
{
    Task<InventoryReportResponseDto> GetInventoryReportAsync(InventoryReportFilterDto filter);
}
