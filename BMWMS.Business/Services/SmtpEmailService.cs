using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace BMWMS.Business.Services;

public class SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger) : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body)
        => SendEmailAsync(new EmailMessage { To = to, Subject = subject, HtmlBody = body });

    public async Task SendEmailAsync(EmailMessage message)
    {
        var settings = config.GetSection("SmtpSettings");
        var host = settings["Host"];
        var username = settings["Username"];
        var password = settings["Password"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Cấu hình SMTP chưa đầy đủ nên email chưa được gửi.");
        if (!int.TryParse(settings["Port"] ?? "587", out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException("Cổng SMTP không hợp lệ.");
        if (!bool.TryParse(settings["EnableSsl"] ?? "true", out var ssl))
            throw new InvalidOperationException("Cấu hình SSL của SMTP không hợp lệ.");
        if (string.IsNullOrWhiteSpace(message.To)) throw new ArgumentException("Email người nhận không hợp lệ.");
        using var client = new SmtpClient(host, port) { Credentials = new NetworkCredential(username, password), EnableSsl = ssl };
        using var mail = new MailMessage {
            From = new MailAddress(settings["FromEmail"] ?? username, "BMWMS"),
            Subject = message.Subject, Body = message.HtmlBody, IsBodyHtml = true
        };
        mail.To.Add(message.To);
        foreach (var attachment in message.Attachments)
        {
            if (attachment.Content.Length > 0)
                mail.Attachments.Add(new Attachment(new MemoryStream(attachment.Content, writable: false), attachment.FileName, attachment.ContentType));
        }
        var timeoutSeconds = int.TryParse(settings["TimeoutSeconds"], out var configuredTimeout)
            ? Math.Clamp(configuredTimeout, 10, 90) : 30;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            // The caller must observe delivery errors before confirming an order.
            await client.SendMailAsync(mail, timeout.Token);
            logger.LogInformation("SMTP accepted email to {Recipient}", message.To);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SMTP failed or timed out for {Recipient}", message.To);
            throw new InvalidOperationException(GetFailureMessage(ex), ex);
        }
    }
    public static string GetFailureMessage(Exception error)
    {
        var smtp = error as SmtpException ?? error.InnerException as SmtpException;
        var reply = smtp?.Message ?? string.Empty;
        // Never show raw provider replies or credentials in user-facing errors.
        if (reply.Contains("5.7.9", StringComparison.OrdinalIgnoreCase)
            || reply.Contains("app password", StringComparison.OrdinalIgnoreCase)
            || reply.Contains("application-specific password", StringComparison.OrdinalIgnoreCase))
            return "Email chưa được gửi: Gmail yêu cầu mật khẩu ứng dụng (App Password). Quản trị viên cần cập nhật xác thực SMTP và khởi động lại API.";
        if (reply.Contains("534") || reply.Contains("535") || reply.Contains("5.7.8")
            || reply.Contains("authentication", StringComparison.OrdinalIgnoreCase)
            || reply.Contains("not authenticated", StringComparison.OrdinalIgnoreCase))
            return "Email chưa được gửi: máy chủ SMTP từ chối xác thực tài khoản. Quản trị viên cần kiểm tra tài khoản và mật khẩu ứng dụng SMTP.";
        if (error is SmtpFailedRecipientException)
            return "Máy chủ SMTP từ chối địa chỉ người nhận. Kiểm tra email nhà cung cấp trước khi gửi lại.";
        return "SMTP chưa xác nhận gửi thành công do lỗi kết nối, TLS hoặc quá thời gian chờ. Kiểm tra hộp thư nhận trước khi gửi lại để tránh gửi trùng.";
    }
}
