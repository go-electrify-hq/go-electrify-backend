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

        public async Task SendOtpAsync(string toEmail, string otp, CancellationToken ct = default)
        {
            var subject = "[Go Electrify] Your OTP code";
            var htmlBody = $"<p>Your login code is <b>{otp}</b>. It expires in 5 minutes.</p>";
            await SendAsync(toEmail, subject, htmlBody, ct); // Reuse the existing SendAsync method
        }
    }
}
