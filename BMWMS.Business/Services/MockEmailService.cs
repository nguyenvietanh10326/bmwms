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
        _logger.LogInformation("\n========== GỬI EMAIL GIẢ LẬP ==========\nĐến: {To}\nTiêu đề: {Subject}\nNội dung: {Body}\n========================================\n", 
            to, subject, body);
        return Task.CompletedTask;
    }
}
