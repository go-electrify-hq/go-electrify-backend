using GoElectrify.BLL.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace GoElectrify.DAL.Infra
{
    public class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            logger.LogInformation("EMAIL -> {to}\nSUBJECT: {sub}\nBODY:\n{body}", toEmail, subject, htmlBody);
            return Task.CompletedTask;
        }
    }
}
