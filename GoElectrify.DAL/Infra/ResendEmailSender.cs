using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using GoElectrify.BLL.Contracts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;


namespace GoElectrify.DAL.Infra
{
    public sealed class EmailSenderOptions
    {
        public string From { get; set; } = "Go Electrify <no-reply@yourdomain.com>";
    }

    public sealed class ResendEmailSender : IEmailSender
    {
        private readonly IResend _resend;                   // SDK chính thức
        private readonly EmailSenderOptions _opts;
        private readonly ILogger<ResendEmailSender> _log;

        public ResendEmailSender(IResend resend,
                                 IOptions<EmailSenderOptions> opts,
                                 ILogger<ResendEmailSender> log)
        {
            _resend = resend;
            _opts = opts.Value;
            _log = log;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            var msg = new EmailMessage
            {
                From = _opts.From,               // cấu hình “From” tập trung
                Subject = subject,
                HtmlBody = htmlBody
            };
            msg.To.Add(toEmail);                 // SDK yêu cầu add vào collection

            // Theo docs: dùng IResend.EmailSendAsync(...)
            var result = await _resend.EmailSendAsync(msg);

            if (result.Content is Guid messageId)
            {
                _log.LogInformation("Resend sent email to {To}. MessageId={Id}", toEmail, messageId);
            }
            else
            {
                _log.LogWarning("Resend sent email to {To}, but MessageId is unavailable.", toEmail);
            }
        }
    }
}
