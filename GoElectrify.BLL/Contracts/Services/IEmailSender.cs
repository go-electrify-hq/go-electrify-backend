namespace GoElectrify.BLL.Contracts.Services
{
    public interface IEmailSender
    {
        /// <summary>Gửi email HTML đơn giản (dev có thể log ra console).</summary>
        Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
    }
}
