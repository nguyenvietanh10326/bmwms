using BMWMS.Business.DTOs.Report;
using BMWMS.Business.Interfaces;
using ClosedXML.Excel;
using System.Reflection;

namespace BMWMS.Business.Services;

public class ExcelExportService : IExcelExportService
{
    public byte[] ExportInventory(IEnumerable<InventoryReportItemDto> items)
    {
        return GenerateExcel(items, "Inventory Report");
    }

    public byte[] ExportInbound(IEnumerable<InboundReportItemDto> items)
    {
        return GenerateExcel(items, "Inbound Report");
    }

    public byte[] ExportOutbound(IEnumerable<OutboundReportItemDto> items)
    {
        return GenerateExcel(items, "Outbound Report");
    }

    public byte[] ExportInOutStock(IEnumerable<InOutStockReportItemDto> items)
    {
        return GenerateExcel(items, "In-Out Stock Report");
    }

    public byte[] ExportProductStatistics(IEnumerable<ProductStatisticsItemDto> items)
    {
        return GenerateExcel(items, "Product Statistics");
    }

    public byte[] ExportSupplierStatistics(IEnumerable<SupplierStatisticsItemDto> items)
    {
        return GenerateExcel(items, "Supplier Statistics");
    }

    public byte[] ExportStocktakeStatistics(IEnumerable<StocktakeStatisticsItemDto> items)
    {
        return GenerateExcel(items, "Stocktake Statistics");
    }

    public byte[] ExportLowStockAlerts(IEnumerable<LowStockAlertItemDto> items)
    {
        return GenerateExcel(items, "Low Stock Alerts");
    }

    public byte[] ExportExpiringLotAlerts(IEnumerable<ExpiringLotAlertItemDto> items)
    {
        return GenerateExcel(items, "Expiring Lot Alerts");
    }

    private byte[] GenerateExcel<T>(IEnumerable<T> data, string sheetName)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(sheetName);

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Headers
        for (int i = 0; i < properties.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = properties[i].Name;
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data
        var dataList = data.ToList();
        for (int i = 0; i < dataList.Count; i++)
        {
            for (int j = 0; j < properties.Length; j++)
            {
                var value = properties[j].GetValue(dataList[i]);
                worksheet.Cell(i + 2, j + 1).Value = value?.ToString() ?? "";
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
