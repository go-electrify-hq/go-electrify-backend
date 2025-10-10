using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.ChargingSession;
using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Services
{
    public sealed class ChargingSessionService(IChargingSessionRepository repo, IBookingRepository bookingRepo) : IChargingSessionService
    {
        private const int SLOT_MINUTES = 30;

        public async Task<ChargingSessionDto> StartForDriverAsync(int userId, StartSessionDto dto, CancellationToken ct)
        {
            // 1) Lấy charger và dùng nó làm nguồn StationId/ConnectorTypeId
            var charger = await repo.GetChargerAsync(dto.ChargerId, ct)
                ?? throw new InvalidOperationException("Charger not found.");
            if (string.Equals(charger.Status, "OFFLINE", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Charger is offline.");

            var stationId = charger.StationId;
            var connectorTypeId = charger.ConnectorTypeId;

            // 2) Vehicle phải hỗ trợ connector của CHARGER
            var okCompat = await repo.VehicleSupportsConnectorAsync(dto.VehicleModelId, connectorTypeId, ct);
            if (!okCompat) throw new InvalidOperationException("Vehicle not compatible with this connector.");

            // 3) Nếu có booking code → kiểm tra match với charger
            Booking? booking = null;
            if (!string.IsNullOrWhiteSpace(dto.BookingCode))
            {
                booking = await repo.FindBookingByCodeForUserAsync(dto.BookingCode!, userId, ct)
                          ?? throw new InvalidOperationException("Booking not found for this user.");

                if (booking.Status is "CANCELED" or "EXPIRED" or "CONSUMED")
                    throw new InvalidOperationException($"Booking is not usable: {booking.Status}.");

                var expireAt = booking.ScheduledStart.AddMinutes(SLOT_MINUTES + 10);
                if (DateTime.UtcNow >= expireAt) throw new InvalidOperationException("Booking has expired.");

                if (booking.StationId != stationId || booking.ConnectorTypeId != connectorTypeId)
                    throw new InvalidOperationException("Booking does not match this charger.");
            }

            // 4) Transaction + re-check charger rảnh
            await using var tx = await repo.BeginSerializableTxAsync(ct);
            if (await repo.CountActiveOnChargerAsync(charger.Id, ct) > 0)
                throw new InvalidOperationException("This charger is currently in use.");

            // 5) Tạo session 
            var session = new ChargingSession
            {
                ChargerId = charger.Id,
                BookingId = booking?.Id,
                StartedAt = DateTime.UtcNow,
                Status = "RUNNING",
                SocStart = dto.InitialSoc,
            };

            await repo.AddSessionAsync(session, ct);

            if (booking is not null)
            {
                booking.Status = "CONSUMED";
                booking.UpdatedAt = DateTime.UtcNow;
                await bookingRepo.UpdateAsync(booking, ct);
            }

            await repo.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // 6) Trả DTO: lấy StationId/ConnectorTypeId từ CHARGER
            return new ChargingSessionDto
            {
                Id = session.Id,
                Status = session.Status,
                StartedAt = session.StartedAt,
                ChargerId = session.ChargerId,
                StationId = stationId,
                ConnectorTypeId = connectorTypeId,
                BookingId = session.BookingId
            };
        }

        public async Task<ChargingSessionDto> StopAsync(int userId, int sessionId, string reason, CancellationToken ct)
        {
            var s = await repo.GetSessionAsync(sessionId, ct) ?? throw new InvalidOperationException("Session not found.");
            if (s.EndedAt is null)
            {
                s.EndedAt = DateTime.UtcNow;
                s.Status = "STOPPED";
                s.DurationMinutes = (int)Math.Max(0, (s.EndedAt.Value - s.StartedAt).TotalMinutes);
                await repo.SaveChangesAsync(ct);
            }

            // Lấy charger để trả StationId/ConnectorTypeId
            var charger = await repo.GetChargerAsync(s.ChargerId, ct) ?? throw new InvalidOperationException("Charger not found.");
            return new ChargingSessionDto
            {
                Id = s.Id,
                Status = s.Status!,
                StartedAt = s.StartedAt,
                ChargerId = s.ChargerId,
                StationId = charger.StationId,
                ConnectorTypeId = charger.ConnectorTypeId,
                BookingId = s.BookingId
            };
        }
    }
}
