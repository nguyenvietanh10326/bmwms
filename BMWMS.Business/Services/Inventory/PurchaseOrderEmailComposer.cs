using System.Globalization;
using System.Net;
using System.Text;
using BMWMS.Repository.Models;
using ClosedXML.Excel;

namespace BMWMS.Business.Services.Inventory;

public interface IPurchaseOrderEmailComposer
{
    EmailMessage Compose(PurchaseOrder order, string confirmUrl, string cancelUrl);
    EmailMessage ComposeRemainderRequest(
        PurchaseOrder order,
        IReadOnlyCollection<PurchaseOrderRemainderLine> lines,
        DateOnly requestedDeliveryDate,
        string? managerNote,
        string confirmUrl,
        string cancelUrl);
    EmailMessage ComposePartialClosureNotice(
        PurchaseOrder order,
        IReadOnlyCollection<PurchaseOrderRemainderLine> lines,
        string reason);
    EmailMessage ComposeWithdrawalNotice(PurchaseOrder order, string reason);
}

public sealed record PurchaseOrderRemainderLine(
    string ProductCode,
    string ProductName,
    string UnitName,
    byte QuantityScale,
    decimal OrderedQuantity,
    decimal ReceivedQuantity,
    decimal RejectedQuantity,
    decimal RemainingQuantity);

public sealed class PurchaseOrderEmailComposer : IPurchaseOrderEmailComposer
{
    public EmailMessage Compose(PurchaseOrder order, string confirmUrl, string cancelUrl)
    {
        ArgumentNullException.ThrowIfNull(order);

        var supplierName = Encode(order.Supplier?.SupplierName ?? "Quý Nhà cung cấp");
        var poNumber = Encode(order.PurchaseOrderNumber);
        var safeConfirmUrl = Encode(confirmUrl);
        var safeCancelUrl = Encode(cancelUrl);
        var rows = new StringBuilder();
        var lineNumber = 1;

        foreach (var detail in order.PurchaseOrderDetails.OrderBy(d => d.PurchaseOrderDetailId))
        {
            var product = detail.Product;
            var scale = product?.UnitOfMeasure?.QuantityScale ?? 0;
            rows.Append("<tr>")
                .Append($"<td style=\"padding:8px;border:1px solid #d8dee9;text-align:center\">{lineNumber++}</td>")
                .Append($"<td style=\"padding:8px;border:1px solid #d8dee9\">{Encode(product?.ProductCode ?? string.Empty)}</td>")
                .Append($"<td style=\"padding:8px;border:1px solid #d8dee9\">{Encode(product?.ProductName ?? string.Empty)}</td>")
                .Append($"<td style=\"padding:8px;border:1px solid #d8dee9;text-align:right\">{FormatQuantity(detail.OrderedQuantity, scale)}</td>")
                .Append($"<td style=\"padding:8px;border:1px solid #d8dee9\">{Encode(product?.UnitOfMeasure?.UnitName ?? product?.UnitOfMeasure?.UnitCode ?? string.Empty)}</td>")
                .Append($"<td style=\"padding:8px;border:1px solid #d8dee9\">{Encode(detail.Notes ?? string.Empty)}</td>")
                .Append("</tr>");
        }

        var html = $"""
            <!doctype html>
            <html lang="vi">
            <body style="font-family:Arial,sans-serif;color:#172033;line-height:1.5">
              <div style="max-width:900px;margin:0 auto">
                <h2 style="color:#233a6b">Đơn đặt hàng {poNumber}</h2>
                <p>Kính gửi <strong>{supplierName}</strong>,</p>
                <p>BMWMS gửi thông tin đơn đặt hàng để Nhà cung cấp kiểm tra và phối hợp giao vật tư.</p>
                <table style="margin:16px 0;border-collapse:collapse">
                  <tr><td style="padding:4px 18px 4px 0;color:#687086">Ngày đặt hàng</td><td><strong>{order.OrderDate:dd/MM/yyyy}</strong></td></tr>
                  <tr><td style="padding:4px 18px 4px 0;color:#687086">Ngày giao dự kiến</td><td><strong>{order.ExpectedDeliveryDate?.ToString("dd/MM/yyyy") ?? "Chưa xác định"}</strong></td></tr>
                  <tr><td style="padding:4px 18px 4px 0;color:#687086">Số dòng vật tư</td><td><strong>{order.PurchaseOrderDetails.Count}</strong></td></tr>
                </table>
                <table style="width:100%;border-collapse:collapse;font-size:14px">
                  <thead>
                    <tr style="background:#eef2f8;color:#233a6b">
                      <th style="padding:8px;border:1px solid #d8dee9">STT</th>
                      <th style="padding:8px;border:1px solid #d8dee9">Mã vật tư</th>
                      <th style="padding:8px;border:1px solid #d8dee9">Tên vật tư</th>
                      <th style="padding:8px;border:1px solid #d8dee9">Số lượng</th>
                      <th style="padding:8px;border:1px solid #d8dee9">Đơn vị</th>
                      <th style="padding:8px;border:1px solid #d8dee9">Ghi chú</th>
                    </tr>
                  </thead>
                  <tbody>{rows}</tbody>
                </table>
                <div style="margin:24px 0;padding:18px;background:#f8fafc;border:1px solid #d8dee9;border-radius:8px">
                  <p style="margin:0 0 14px"><strong>Phản hồi đơn đặt hàng</strong></p>
                  <p style="margin:0 0 16px;color:#526079">Vui lòng chọn một trong hai phương án. Hệ thống chỉ ghi nhận phản hồi khi PO vẫn đang chờ xác nhận.</p>
                  <a href="{safeConfirmUrl}" style="display:inline-block;margin-right:10px;padding:10px 18px;border-radius:6px;background:#198754;color:#fff;text-decoration:none;font-weight:700">Xác nhận PO</a>
                  <a href="{safeCancelUrl}" style="display:inline-block;padding:10px 18px;border-radius:6px;background:#dc3545;color:#fff;text-decoration:none;font-weight:700">Từ chối PO</a>
                </div>
                <p style="margin-top:18px">File Excel chi tiết được đính kèm trong email này.</p>
                <p>Trân trọng,<br><strong>BMWMS</strong></p>
              </div>
            </body>
            </html>
            """;

        return new EmailMessage
        {
            To = order.Supplier?.Email?.Trim() ?? string.Empty,
            Subject = $"Đơn đặt hàng {order.PurchaseOrderNumber}",
            HtmlBody = html,
            Attachments = new[]
            {
                new EmailAttachment(
                    $"PO-{SanitizeFileName(order.PurchaseOrderNumber)}.xlsx",
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    BuildWorkbook(order))
            }
        };
    }

    public EmailMessage ComposeRemainderRequest(
        PurchaseOrder order,
        IReadOnlyCollection<PurchaseOrderRemainderLine> lines,
        DateOnly requestedDeliveryDate,
        string? managerNote,
        string confirmUrl,
        string cancelUrl)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(lines);

        var rows = BuildRemainderHtmlRows(lines);
        var noteHtml = string.IsNullOrWhiteSpace(managerNote)
            ? string.Empty
            : $"<p><strong>Ghi chú của Quản lý kho:</strong> {Encode(managerNote.Trim())}</p>";
        var html = $"""
            <!doctype html>
            <html lang="vi">
            <body style="font-family:Arial,sans-serif;color:#172033;line-height:1.5">
              <div style="max-width:980px;margin:0 auto">
                <h2 style="color:#233a6b">Đề nghị giao phần còn lại của PO {Encode(order.PurchaseOrderNumber)}</h2>
                <p>Kính gửi <strong>{Encode(order.Supplier?.SupplierName ?? "Quý Nhà cung cấp")}</strong>,</p>
                <p>BMWMS đã ghi nhận một phần hàng của PO. Bảng dưới đây đối chiếu số đặt, số đạt đã nhập kho, số không đạt khi giao nhận và số lượng còn đề nghị giao.</p>
                <p><strong>Ngày giao tiếp theo đề nghị:</strong> {requestedDeliveryDate:dd/MM/yyyy}</p>
                {noteHtml}
                <table style="width:100%;border-collapse:collapse;font-size:14px">
                  <thead><tr style="background:#eef2f8;color:#233a6b">
                    <th style="padding:8px;border:1px solid #d8dee9">Mã vật tư</th>
                    <th style="padding:8px;border:1px solid #d8dee9">Tên vật tư</th>
                    <th style="padding:8px;border:1px solid #d8dee9">Đơn vị</th>
                    <th style="padding:8px;border:1px solid #d8dee9">Đã đặt</th>
                    <th style="padding:8px;border:1px solid #d8dee9">Đã nhận đạt</th>
                    <th style="padding:8px;border:1px solid #d8dee9">Không đạt</th>
                    <th style="padding:8px;border:1px solid #d8dee9">Còn đề nghị giao</th>
                  </tr></thead>
                  <tbody>{rows}</tbody>
                </table>
                <div style="margin:24px 0;padding:18px;background:#f8fafc;border:1px solid #d8dee9;border-radius:8px">
                  <p style="margin:0 0 14px"><strong>Phản hồi phần còn lại của PO</strong></p>
                  <p style="margin:0 0 16px;color:#526079">Vui lòng xác nhận khả năng tiếp tục giao đúng phần số lượng còn lại, hoặc thông báo không thể tiếp tục giao.</p>
                  <a href="{Encode(confirmUrl)}" style="display:inline-block;margin-right:10px;padding:10px 18px;border-radius:6px;background:#198754;color:#fff;text-decoration:none;font-weight:700">Xác nhận tiếp tục giao</a>
                  <a href="{Encode(cancelUrl)}" style="display:inline-block;padding:10px 18px;border-radius:6px;background:#dc3545;color:#fff;text-decoration:none;font-weight:700">Không thể tiếp tục giao</a>
                </div>
                <p>File Excel đối chiếu được đính kèm trong email này.</p>
                <p>Trân trọng,<br><strong>BMWMS</strong></p>
              </div>
            </body>
            </html>
            """;

        return new EmailMessage
        {
            To = order.Supplier?.Email?.Trim() ?? string.Empty,
            Subject = $"Đề nghị giao phần còn lại - {order.PurchaseOrderNumber}",
            HtmlBody = html,
            Attachments = new[]
            {
                new EmailAttachment(
                    $"PO-{SanitizeFileName(order.PurchaseOrderNumber)}-phan-con-lai.xlsx",
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    BuildRemainderWorkbook(order, lines, "ĐỀ NGHỊ GIAO PHẦN CÒN LẠI", requestedDeliveryDate, managerNote))
            }
        };
    }

    public EmailMessage ComposePartialClosureNotice(
        PurchaseOrder order,
        IReadOnlyCollection<PurchaseOrderRemainderLine> lines,
        string reason)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(lines);

        var html = $"""
            <!doctype html>
            <html lang="vi">
            <body style="font-family:Arial,sans-serif;color:#172033;line-height:1.5">
              <div style="max-width:980px;margin:0 auto">
                <h2 style="color:#233a6b">Thông báo kết thúc phần còn lại của PO {Encode(order.PurchaseOrderNumber)}</h2>
                <p>Kính gửi <strong>{Encode(order.Supplier?.SupplierName ?? "Quý Nhà cung cấp")}</strong>,</p>
                <p>BMWMS xác nhận kết thúc PO theo số lượng thực tế đã nhận đạt và không tiếp tục nhận phần còn lại nêu dưới đây.</p>
                <p><strong>Lý do:</strong> {Encode(reason)}</p>
                <table style="width:100%;border-collapse:collapse;font-size:14px">
                  <thead><tr style="background:#eef2f8;color:#233a6b">
                    <th style="padding:8px;border:1px solid #d8dee9">Mã vật tư</th>
                    <th style="padding:8px;border:1px solid #d8dee9">Tên vật tư</th>
                    <th style="padding:8px;border:1px solid #d8dee9">Đơn vị</th>
                    <th style="padding:8px;border:1px solid #d8dee9">Đã đặt</th>
                    <th style="padding:8px;border:1px solid #d8dee9">Đã nhận đạt</th>
                    <th style="padding:8px;border:1px solid #d8dee9">Không đạt</th>
                    <th style="padding:8px;border:1px solid #d8dee9">Phần kết thúc</th>
                  </tr></thead>
                  <tbody>{BuildRemainderHtmlRows(lines)}</tbody>
                </table>
                <p style="margin-top:20px">Đây là thông báo nghiệp vụ; Nhà cung cấp không cần thao tác xác nhận trên hệ thống.</p>
                <p>File Excel đối chiếu được đính kèm trong email này.</p>
                <p>Trân trọng,<br><strong>BMWMS</strong></p>
              </div>
            </body>
            </html>
            """;

        return new EmailMessage
        {
            To = order.Supplier?.Email?.Trim() ?? string.Empty,
            Subject = $"Thông báo kết thúc phần còn lại - {order.PurchaseOrderNumber}",
            HtmlBody = html,
            Attachments = new[]
            {
                new EmailAttachment(
                    $"PO-{SanitizeFileName(order.PurchaseOrderNumber)}-ket-thuc.xlsx",
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    BuildRemainderWorkbook(order, lines, "KẾT THÚC PHẦN CÒN LẠI", null, reason))
            }
        };
    }

    public EmailMessage ComposeWithdrawalNotice(PurchaseOrder order, string reason)
    {
        ArgumentNullException.ThrowIfNull(order);
        var html = $"""
            <!doctype html>
            <html lang="vi">
            <body style="font-family:Arial,sans-serif;color:#172033;line-height:1.5">
              <div style="max-width:760px;margin:0 auto">
                <h2 style="color:#233a6b">Thông báo rút PO {Encode(order.PurchaseOrderNumber)}</h2>
                <p>Kính gửi <strong>{Encode(order.Supplier?.SupplierName ?? "Quý Nhà cung cấp")}</strong>,</p>
                <p>BMWMS xin rút đơn đặt hàng này trong khi chưa nhận được xác nhận của Nhà cung cấp.</p>
                <p><strong>Lý do:</strong> {Encode(reason)}</p>
                <p>Các liên kết xác nhận hoặc từ chối trong email PO trước đó không còn hiệu lực.</p>
                <p>Trân trọng,<br><strong>BMWMS</strong></p>
              </div>
            </body>
            </html>
            """;
        return new EmailMessage
        {
            To = order.Supplier?.Email?.Trim() ?? string.Empty,
            Subject = $"Thông báo rút đơn đặt hàng {order.PurchaseOrderNumber}",
            HtmlBody = html
        };
    }

    private static string BuildRemainderHtmlRows(IEnumerable<PurchaseOrderRemainderLine> lines)
    {
        var rows = new StringBuilder();
        foreach (var line in lines.Where(line => line.RemainingQuantity > 0))
        {
            rows.Append("<tr>")
                .Append($"<td style=\"padding:8px;border:1px solid #d8dee9\">{Encode(line.ProductCode)}</td>")
                .Append($"<td style=\"padding:8px;border:1px solid #d8dee9\">{Encode(line.ProductName)}</td>")
                .Append($"<td style=\"padding:8px;border:1px solid #d8dee9\">{Encode(line.UnitName)}</td>")
                .Append($"<td style=\"padding:8px;border:1px solid #d8dee9;text-align:right\">{FormatQuantity(line.OrderedQuantity, line.QuantityScale)}</td>")
                .Append($"<td style=\"padding:8px;border:1px solid #d8dee9;text-align:right\">{FormatQuantity(line.ReceivedQuantity, line.QuantityScale)}</td>")
                .Append($"<td style=\"padding:8px;border:1px solid #d8dee9;text-align:right\">{FormatQuantity(line.RejectedQuantity, line.QuantityScale)}</td>")
                .Append($"<td style=\"padding:8px;border:1px solid #d8dee9;text-align:right;font-weight:700\">{FormatQuantity(line.RemainingQuantity, line.QuantityScale)}</td>")
                .Append("</tr>");
        }
        return rows.ToString();
    }

    private static byte[] BuildRemainderWorkbook(
        PurchaseOrder order,
        IReadOnlyCollection<PurchaseOrderRemainderLine> lines,
        string title,
        DateOnly? requestedDeliveryDate,
        string? note)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Đối chiếu PO");
        worksheet.Cell("A1").Value = title;
        worksheet.Range("A1:G1").Merge().Style.Font.SetBold().Font.SetFontSize(16)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        worksheet.Cell("A3").Value = "Mã PO";
        worksheet.Cell("B3").Value = order.PurchaseOrderNumber;
        worksheet.Cell("A4").Value = "Nhà cung cấp";
        worksheet.Cell("B4").Value = order.Supplier?.SupplierName ?? string.Empty;
        worksheet.Cell("A5").Value = requestedDeliveryDate.HasValue ? "Ngày giao tiếp theo đề nghị" : "Lý do";
        worksheet.Cell("B5").Value = requestedDeliveryDate.HasValue
            ? requestedDeliveryDate.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
            : note ?? string.Empty;
        if (requestedDeliveryDate.HasValue && !string.IsNullOrWhiteSpace(note))
        {
            worksheet.Cell("A6").Value = "Ghi chú";
            worksheet.Cell("B6").Value = note;
        }

        const int headerRow = 8;
        var headers = new[] { "Mã vật tư", "Tên vật tư", "Đơn vị", "Đã đặt", "Đã nhận đạt", "Không đạt", "Còn lại" };
        for (var column = 1; column <= headers.Length; column++)
            worksheet.Cell(headerRow, column).Value = headers[column - 1];
        worksheet.Range(headerRow, 1, headerRow, headers.Length).Style.Font.SetBold()
            .Fill.SetBackgroundColor(XLColor.FromHtml("#DCE6F1"));

        var row = headerRow + 1;
        foreach (var line in lines.Where(line => line.RemainingQuantity > 0))
        {
            worksheet.Cell(row, 1).Value = line.ProductCode;
            worksheet.Cell(row, 2).Value = line.ProductName;
            worksheet.Cell(row, 3).Value = line.UnitName;
            worksheet.Cell(row, 4).Value = line.OrderedQuantity;
            worksheet.Cell(row, 5).Value = line.ReceivedQuantity;
            worksheet.Cell(row, 6).Value = line.RejectedQuantity;
            worksheet.Cell(row, 7).Value = line.RemainingQuantity;
            var numberFormat = line.QuantityScale == 0 ? "0" : $"0.{new string('#', line.QuantityScale)}";
            worksheet.Range(row, 4, row, 7).Style.NumberFormat.Format = numberFormat;
            row++;
        }

        var lastRow = Math.Max(headerRow, row - 1);
        worksheet.Range(headerRow, 1, lastRow, headers.Length).Style.Border
            .SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetInsideBorder(XLBorderStyleValues.Thin);
        worksheet.SheetView.FreezeRows(headerRow);
        worksheet.Columns().AdjustToContents(8, 42);
        worksheet.Column(2).Width = Math.Min(42, Math.Max(18, worksheet.Column(2).Width));
        worksheet.Column(2).Style.Alignment.WrapText = true;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static byte[] BuildWorkbook(PurchaseOrder order)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Purchase Order");

        worksheet.Cell("A1").Value = "ĐƠN ĐẶT HÀNG";
        worksheet.Range("A1:F1").Merge().Style
            .Font.SetBold()
            .Font.SetFontSize(16)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        worksheet.Cell("A3").Value = "Mã PO";
        worksheet.Cell("B3").Value = order.PurchaseOrderNumber;
        worksheet.Cell("A4").Value = "Nhà cung cấp";
        worksheet.Cell("B4").Value = order.Supplier?.SupplierName ?? string.Empty;
        worksheet.Cell("A5").Value = "Ngày đặt hàng";
        worksheet.Cell("B5").Value = order.OrderDate.ToDateTime(TimeOnly.MinValue);
        worksheet.Cell("B5").Style.DateFormat.Format = "dd/MM/yyyy";
        worksheet.Cell("A6").Value = "Ngày giao dự kiến";
        if (order.ExpectedDeliveryDate.HasValue)
        {
            worksheet.Cell("B6").Value = order.ExpectedDeliveryDate.Value.ToDateTime(TimeOnly.MinValue);
            worksheet.Cell("B6").Style.DateFormat.Format = "dd/MM/yyyy";
        }
        else
        {
            worksheet.Cell("B6").Value = "Chưa xác định";
        }

        var headerRow = 8;
        var headers = new[] { "STT", "Mã vật tư", "Tên vật tư", "Số lượng", "Đơn vị", "Ghi chú" };
        for (var column = 1; column <= headers.Length; column++)
            worksheet.Cell(headerRow, column).Value = headers[column - 1];
        worksheet.Range(headerRow, 1, headerRow, headers.Length).Style
            .Font.SetBold()
            .Fill.SetBackgroundColor(XLColor.FromHtml("#DCE6F1"));

        var row = headerRow + 1;
        foreach (var detail in order.PurchaseOrderDetails.OrderBy(d => d.PurchaseOrderDetailId))
        {
            var product = detail.Product;
            var scale = product?.UnitOfMeasure?.QuantityScale ?? 0;
            worksheet.Cell(row, 1).Value = row - headerRow;
            worksheet.Cell(row, 2).Value = product?.ProductCode ?? string.Empty;
            worksheet.Cell(row, 3).Value = product?.ProductName ?? string.Empty;
            worksheet.Cell(row, 4).Value = detail.OrderedQuantity;
            worksheet.Cell(row, 4).Style.NumberFormat.Format = scale == 0 ? "0" : $"0.{new string('#', scale)}";
            worksheet.Cell(row, 5).Value = product?.UnitOfMeasure?.UnitName ?? product?.UnitOfMeasure?.UnitCode ?? string.Empty;
            worksheet.Cell(row, 6).Value = detail.Notes ?? string.Empty;
            row++;
        }

        var lastRow = Math.Max(headerRow, row - 1);
        var tableRange = worksheet.Range(headerRow, 1, lastRow, headers.Length);
        tableRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            .Border.SetInsideBorder(XLBorderStyleValues.Thin);
        if (row > headerRow + 1)
            tableRange.CreateTable("PurchaseOrderLines");
        worksheet.SheetView.FreezeRows(headerRow);
        worksheet.Columns().AdjustToContents(8, 42);
        worksheet.Column(3).Width = Math.Min(42, Math.Max(18, worksheet.Column(3).Width));
        worksheet.Column(6).Width = Math.Min(42, Math.Max(18, worksheet.Column(6).Width));
        worksheet.Column(3).Style.Alignment.WrapText = true;
        worksheet.Column(6).Style.Alignment.WrapText = true;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string FormatQuantity(decimal quantity, byte scale)
    {
        var format = scale == 0 ? "0" : $"0.{new string('#', scale)}";
        return quantity.ToString(format, CultureInfo.InvariantCulture);
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(value.Select(character => invalid.Contains(character) ? '_' : character));
    }
}
