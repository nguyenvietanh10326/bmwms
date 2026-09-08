using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BMWMS.Business.Services;

public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        return SendEmailAsync(new EmailMessage
        {
            To = to,
            Subject = subject,
            HtmlBody = body
        });
    }

    public Task SendEmailAsync(EmailMessage message)
    {
        _logger.LogInformation(
            "Email giả lập tới {Recipient}, tiêu đề {Subject}, số tệp đính kèm {AttachmentCount}.",
            message.To,
            message.Subject,
            message.Attachments.Count);
        return Task.CompletedTask;
    }
}
