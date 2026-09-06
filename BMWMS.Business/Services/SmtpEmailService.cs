using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BMWMS.Business.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        await SendEmailAsync(new EmailMessage
        {
            To = to,
            Subject = subject,
            HtmlBody = body
        });
    }

    public async Task SendEmailAsync(EmailMessage message)
    {
        var smtpConfig = _config.GetSection("SmtpSettings");
        var host = smtpConfig["Host"];
        var port = int.Parse(smtpConfig["Port"] ?? "587");
        var username = smtpConfig["Username"];
        var password = smtpConfig["Password"];
        var enableSsl = bool.Parse(smtpConfig["EnableSsl"] ?? "true");
        var fromEmail = smtpConfig["FromEmail"];

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("SMTP Settings are not fully configured. Email was not sent.");
            throw new InvalidOperationException("Cấu hình SMTP chưa đầy đủ nên email chưa được gửi.");
        }

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = enableSsl,
            Timeout = 10000 // 10 seconds
        };

        if (string.IsNullOrWhiteSpace(message.To))
            throw new ArgumentException("Địa chỉ email người nhận không hợp lệ.", nameof(message));

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail ?? username, "BMWMS System"),
            Subject = message.Subject,
            Body = message.HtmlBody,
            IsBodyHtml = true,
        };

        mailMessage.To.Add(message.To);

        foreach (var attachment in message.Attachments)
        {
            if (attachment.Content.Length == 0) continue;
            var stream = new MemoryStream(attachment.Content, writable: false);
            mailMessage.Attachments.Add(new Attachment(stream, attachment.FileName, attachment.ContentType));
        }

        try
        {
            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Sent email successfully to {Recipient}", message.To);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", message.To);
            throw;
        }
    }
}
