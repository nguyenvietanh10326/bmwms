using BMWMS.Business.DTOs.Report;

namespace BMWMS.Business.Interfaces;

public interface IExcelExportService
{
    byte[] ExportInventory(IEnumerable<InventoryReportItemDto> items);
    byte[] ExportInbound(IEnumerable<InboundReportItemDto> items);
    byte[] ExportOutbound(IEnumerable<OutboundReportItemDto> items);
    byte[] ExportInOutStock(IEnumerable<InOutStockReportItemDto> items);
    byte[] ExportSupplierStatistics(IEnumerable<SupplierStatisticsItemDto> items);
    byte[] ExportLowStockAlerts(IEnumerable<LowStockAlertItemDto> items);
    byte[] ExportExpiringLotAlerts(IEnumerable<ExpiringLotAlertItemDto> items);
}
