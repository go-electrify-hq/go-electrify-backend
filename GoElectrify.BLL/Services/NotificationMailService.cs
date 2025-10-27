using GoElectrify.BLL.Contracts.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Services
{
    public class NotificationMailService : INotificationMailService
    {
        private readonly IEmailSender _email;

        public NotificationMailService(IEmailSender email)
        {
            _email = email;
        }

        public Task SendTopupSuccessAsync(
            string toEmail,
            decimal amount,
            string provider,
            string orderCode,
            DateTime completedAtUtc,
            CancellationToken ct = default)
        {
            var vnd = amount.ToString("N0", new CultureInfo("vi-VN"));
            var time = completedAtUtc.ToLocalTime().ToString("HH:mm dd/MM/yyyy");

            var subject = "[GoElectrify] Nạp ví thành công";
            var html = $@"
<!doctype html>
<html><body style=""font-family:Segoe UI,Arial,sans-serif"">
  <h2>Nạp ví thành công</h2>
  <p>Số tiền: <b>{vnd}₫</b></p>
  <p>Nhà cung cấp: <b>{provider}</b></p>
  <p>Mã đơn: <b>{orderCode}</b></p>
  <p>Thời gian: {time}</p>
  <hr>
  <p>Cảm ơn bạn đã sử dụng GoElectrify.</p>
</body></html>";

            return _email.SendAsync(toEmail, subject, html, ct);
        }

        public Task SendBookingSuccessAsync(
            string toEmail,
            string bookingCode,
            string stationName,
            string? chargerName,
            DateTime startTimeUtc,
            DateTime? endTimeUtc,
            CancellationToken ct = default)
        {
            var start = startTimeUtc.ToLocalTime().ToString("HH:mm dd/MM/yyyy");
            var end = endTimeUtc?.ToLocalTime().ToString("HH:mm dd/MM/yyyy");
            var subject = "[GoElectrify] Đặt chỗ thành công";

            var sb = new StringBuilder($@"
<!doctype html>
<html><body style=""font-family:Segoe UI,Arial,sans-serif"">
  <h2>Đặt chỗ thành công</h2>
  <p>Mã đặt chỗ: <b>{System.Net.WebUtility.HtmlEncode(bookingCode)}</b></p>
  <p>Trạm: <b>{System.Net.WebUtility.HtmlEncode(stationName)}</b></p>");
            if (!string.IsNullOrWhiteSpace(chargerName))
                sb.Append($"<p>Charger: <b>{System.Net.WebUtility.HtmlEncode(chargerName)}</b></p>");
            sb.Append($"<p>Bắt đầu: {start}</p>");
            if (end != null) sb.Append($"<p>Kết thúc dự kiến: {end}</p>");
            sb.Append(@"
  <hr>
  <p>Chúc bạn có hành trình thuận lợi!</p>
</body></html>");

            return _email.SendAsync(toEmail, subject, sb.ToString(), ct);
        }
    }
}
