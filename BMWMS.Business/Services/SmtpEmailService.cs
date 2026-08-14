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
            return;
        }

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = enableSsl,
            Timeout = 10000 // 10 seconds
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromEmail ?? username, "BMWMS System"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
        };

        mailMessage.To.Add(to);

        try
        {
            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Sent email successfully to {to}", to);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {to}", to);
            // In a real application, you might want to rethrow or handle this differently
        }
    }
}
