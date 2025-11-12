using GoElectrify.BLL.Contracts.Services;
using System;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Services
{
    public class NotificationMailService : INotificationMailService
    {
        private readonly IEmailSender _email;
        public NotificationMailService(IEmailSender email) => _email = email;

        /// <summary>
        /// Email: NẠP VÍ THÀNH CÔNG (Payment)
        /// UI theo yêu cầu: tiêu đề + lời chào + Số tiền/Mã đơn/Thời gian + separator + cảm ơn
        /// </summary>
        public Task SendTopupSuccessAsync(
            string toEmail,
            decimal amount,
            string provider,        // vẫn giữ tham số để không phá interface, nhưng giao diện mới không hiển thị provider
            long orderCode,
            DateTime completedAtUtc,
            CancellationToken ct = default)
        {
            var vi = new CultureInfo("vi-VN");
            var money = string.Format(vi, "{0:C0}", amount) ;                         // ví dụ: 50.000 ₫
            var time = completedAtUtc.ToLocalTime().ToString("HH:mm dd/MM/yyyy");

            var subject = "[Go Electrify] NẠP VÍ THÀNH CÔNG"; // theo yêu cầu mới
            var html = $@"
            <!doctype html>
            <html>
              <body style=""font-family:Segoe UI,Arial,sans-serif; line-height:1.6; color:#111"">
                <h2 style=""margin:0 0 12px"">{subject}</h2>
                <p>Chào quý khách,</p>

                <p>Bạn đã thành công nạp ví cho tài khoản Go Electrify của bạn:</p>
                <ul style=""padding-left:18px; margin:8px 0 12px"">
                  <li>Mã đơn: <b>{WebUtility.HtmlEncode(orderCode.ToString())}</b></li>
                  <li>Số tiền: <b>{WebUtility.HtmlEncode(money)}</b></li>
                  <li>Hình thức: <b>{WebUtility.HtmlEncode(provider)}</b></li>
                  <li>Thời gian: <b>{WebUtility.HtmlEncode(time)}</b></li>
                </ul>

                <hr style=""border:none;height:1px;background:#e5e7eb;margin:16px 0"" />
                <p style=""margin:0"">Cảm ơn bạn đã sử dụng dịch vụ Go Electrify!</p>
              </body>
            </html>";

            return _email.SendAsync(toEmail, subject, html, ct);
        }

        /// <summary>
        /// Email: ĐẶT CHỖ THÀNH CÔNG (Booking)
        /// UI theo yêu cầu: tiêu đề + Kính chào quý khách + cảm ơn + xác nhận + Mã/Trạm/Thời gian + separator + lời chúc
        /// </summary>
        public Task SendBookingSuccessAsync(
            string toEmail,
            string bookingCode,
            string stationName,
            string? chargerName,    // giao diện mới KHÔNG bắt buộc hiển thị charger; vẫn giữ tham số để không phá interface
            DateTime startTimeUtc,
            DateTime? endTimeUtc,
            CancellationToken ct = default)
        {
            var start = startTimeUtc.ToLocalTime().ToString("HH:mm dd/MM/yyyy");
            var end = endTimeUtc?.ToLocalTime().ToString("HH:mm dd/MM/yyyy");

            // Theo giao diện bạn đưa: chỉ 1 dòng "Thời gian"
            var timeText = end is null
                ? start
                : $"{start} - {end}";

            var subject = "[Go Electrify] ĐẶT CHỖ THÀNH CÔNG"; // theo yêu cầu mới

            var sb = new StringBuilder($@"
            <!doctype html>
            <html>
              <body style=""font-family:Segoe UI,Arial,sans-serif; line-height:1.6; color:#111"">
                <h2 style=""margin:0 0 12px"">{subject}</h2>
                <p>Kính chào quý khách</p>

                <p>Cảm ơn bạn đã sử dụng dịch vụ Go Electrify!<br/>
                Go Electrify xác nhận thông tin đặt chỗ của bạn:</p>

                <ul style=""padding-left:18px; margin:8px 0 12px"">
                  <li>Mã đặt chỗ: <b>{WebUtility.HtmlEncode(bookingCode)}</b></li>
                  <li>Trạm: <b>{WebUtility.HtmlEncode(stationName)}</b></li>
                  <li>Thời gian: <b>{WebUtility.HtmlEncode(timeText)}</b></li>
                </ul>

                <hr style=""border:none;height:1px;background:#e5e7eb;margin:16px 0"" />
                <p style=""margin:0"">Chúc bạn có hành trình thuận lợi!</p>
              </body>
            </html>");

            return _email.SendAsync(toEmail, subject, sb.ToString(), ct);
        }

        // Email: MUA GÓI THÀNH CÔNG
        public Task SendSubscriptionPurchaseSuccessAsync(
            string toEmail,
            string planName,
            decimal price,
            string provider,
            string orderCode,
            int durationDays,
            DateTime activatedAtUtc,
            CancellationToken ct = default)
                {
                    var vi = new CultureInfo("vi-VN");
                    var priceStr = string.Format(vi, "{0:C0}", price);
                    var atLocal = activatedAtUtc.ToLocalTime().ToString("HH:mm dd/MM/yyyy");
                    var usedText = durationDays > 0 ? $"{durationDays} ngày" : "Không giới hạn";

                    var subject = "[Go Electrify] MUA GÓI THÀNH CÔNG";
                    var html = $@"
                    <!doctype html>
                    <html>
                      <body style=""font-family:Segoe UI,Arial,sans-serif; line-height:1.6; color:#111"">
                        <h2 style=""margin:0 0 12px"">{subject}</h2>
                        <p>Chào quý khách,</p>

                        <p>Bạn đã mua gói thành công cho tài khoản Go Electrify:</p>
                        <ul style=""padding-left:18px; margin:8px 0 12px"">
                          <li>Gói: <b>{WebUtility.HtmlEncode(planName)}</b></li>
                          <li>Số tiền: <b>{WebUtility.HtmlEncode(priceStr)}</b></li>
                          <li>Hình thức: <b>{WebUtility.HtmlEncode(provider)}</b></li>
                          <li>Mã đơn: <b>{WebUtility.HtmlEncode(orderCode)}</b></li>
                          <li>Thời gian sử dụng: <b>{WebUtility.HtmlEncode(usedText)}</b></li>
                          <li>Thời điểm kích hoạt: <b>{WebUtility.HtmlEncode(atLocal)}</b></li>
                        </ul>

                        <hr style=""border:none;height:1px;background:#e5e7eb;margin:16px 0"" />
                        <p style=""margin:0"">Cảm ơn bạn đã sử dụng dịch vụ Go Electrify!</p>
                      </body>
                    </html>";

            return _email.SendAsync(toEmail, subject, html, ct);
        }

        // Hoàn tất phiên sạc
        public Task SendChargingCompletedAsync(
            string toEmail, 
            string stationName, 
            decimal energyKwh, 
            decimal? cost, 
            DateTime startedAtUtc, 
            DateTime endedAtUtc, 
            CancellationToken ct = default)
        {
                var vi = new CultureInfo("vi-VN");
                var kwh = energyKwh.ToString("N2", vi);
                var money = cost.HasValue ? string.Format(vi, "{0:C0}", cost.Value) : "—";
                var start = startedAtUtc.ToLocalTime().ToString("HH:mm dd/MM/yyyy");
                var end = endedAtUtc.ToLocalTime().ToString("HH:mm dd/MM/yyyy");

                var subject = "[Go Electrify] HOÀN TẤT PHIÊN SẠC";
                var html = $@"
                <!doctype html>
                <html>
                  <body style=""font-family:Segoe UI,Arial,sans-serif; line-height:1.6; color:#111"">
                    <h2 style=""margin:0 0 12px"">{subject}</h2>
                    <p>Chào quý khách,</p>
                    <p>Phiên sạc của bạn đã hoàn tất tại <b>{WebUtility.HtmlEncode(stationName)}</b>:</p>
                    <ul style=""padding-left:18px; margin:8px 0 12px"">
                      <li>Thời gian: <b>{WebUtility.HtmlEncode(start)} - {WebUtility.HtmlEncode(end)}</b></li>
                      <li>Năng lượng: <b>{WebUtility.HtmlEncode(kwh)} kWh</b></li>
                      <li>Chi phí: <b>{WebUtility.HtmlEncode(money)}</b></li>
                    </ul>
                    <hr style=""border:none;height:1px;background:#e5e7eb;margin:16px 0"" />
                    <p style=""margin:0"">Cảm ơn bạn đã sử dụng dịch vụ Go Electrify!</p>
                  </body>
                </html>";

             return _email.SendAsync(toEmail, subject, html, ct);
        }
    }
}
