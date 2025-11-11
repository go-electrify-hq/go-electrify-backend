namespace GoElectrify.BLL.Contracts.Services
{
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);

        Task SendOtpAsync(string toEmail, string otp, CancellationToken ct = default);
    }
}
