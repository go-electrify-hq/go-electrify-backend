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
           string orderCode,
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
    }
}
