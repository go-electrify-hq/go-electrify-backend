using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface INotificationMailService
    {
        Task SendTopupSuccessAsync(
           string toEmail,
           decimal amount,
           string provider,
           long orderCode,
           DateTime completedAtUtc,
           CancellationToken ct = default);

        Task SendBookingSuccessAsync(
            string toEmail,
            string bookingCode,
            string stationName,
            string? chargerName,
            DateTime startTimeUtc,
            DateTime? endTimeUtc,
            CancellationToken ct = default);

        Task SendSubscriptionPurchaseSuccessAsync(
            string toEmail,
            string planName,
            decimal price,        // GIÁ GÓI → hiển thị "Số tiền"
            string provider,      // "Ví ảo"
            string orderCode,     // mã đơn/mã giao dịch nội bộ
            int durationDays,     // THỜI GIAN SỬ DỤNG (ngày)
            DateTime activatedAtUtc,
            CancellationToken ct = default);

        Task SendChargingCompletedAsync(
           string toEmail,
           string stationName,
           decimal energyKwh,
           decimal? cost,
           DateTime startedAtUtc,
           DateTime endedAtUtc,
           CancellationToken ct = default);
    }
}
