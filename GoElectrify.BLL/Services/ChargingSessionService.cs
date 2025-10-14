using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
            // 0) Đọc booking (nếu có)
            Booking? booking = null;
            if (!string.IsNullOrWhiteSpace(dto.BookingCode))
            {
                booking = await repo.FindBookingByCodeForUserAsync(dto.BookingCode!, userId, ct);

                if (booking is null) throw new InvalidOperationException("Booking not found for this user.");
                if (booking.UserId != userId) throw new InvalidOperationException("Booking not owned by user.");
                if (booking.Status is "CANCELED" or "EXPIRED" or "CONSUMED")
                    throw new InvalidOperationException($"Booking is not usable: {booking.Status}.");

                var expireAt = booking.ScheduledStart.AddMinutes(SLOT_MINUTES + 10);
                if (DateTime.UtcNow >= expireAt) throw new InvalidOperationException("Booking has expired.");
            }

            // 1) Xác định CHARGER
            Charger? charger = null;

            if (dto.ChargerId.HasValue)
            {
                charger = await repo.GetChargerAsync(dto.ChargerId.Value, ct)
                    ?? throw new InvalidOperationException("Charger not found.");
                if (string.Equals(charger.Status, "OFFLINE", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Charger is offline.");

                // Nếu có booking => phải khớp station/connector
                if (booking is not null &&
                    (booking.StationId != charger.StationId || booking.ConnectorTypeId != charger.ConnectorTypeId))
                    throw new InvalidOperationException("Booking does not match this charger.");
            }
            else
            {
                // Không gửi ChargerId => bắt buộc có booking để auto-assign
                if (booking is null) throw new InvalidOperationException("ChargerId is required when not using a booking.");

                charger = await repo.FindAvailableChargerAsync(booking.StationId, booking.ConnectorTypeId, ct)
                    ?? throw new InvalidOperationException("No available charger matching this booking.");
            }

            var stationId = charger.StationId;
            var connectorTypeId = charger.ConnectorTypeId;

            // 2) Kiểm tra compatibility Vehicle x Connector
            var okCompat = await repo.VehicleSupportsConnectorAsync(dto.VehicleModelId, connectorTypeId, ct);
            if (!okCompat) throw new InvalidOperationException("Vehicle not compatible with this connector.");

            // 2b) LẤY THÊM số liệu phục vụ giả lập
            //     - VehicleModel: BatteryCapacityKwh, MaxPowerKw
            //     - ConnectorType: MaxPowerKw
            var vehicleModel = await repo.GetVehicleModelAsync(dto.VehicleModelId, ct)
                ?? throw new InvalidOperationException("Vehicle model not found.");

            var connectorType = await repo.GetConnectorTypeAsync(connectorTypeId, ct)
                ?? throw new InvalidOperationException("Connector type not found.");

            // 3) Tạo session 
            var session = new ChargingSession
            {
                ChargerId = charger.Id,
                BookingId = booking?.Id,
                StartedAt = DateTime.UtcNow,
                Status = "RUNNING",
                SocStart = dto.InitialSoc,
            };

            try
            {
                await repo.AddSessionAsync(session, ct);

                if (booking is not null)
                {
                    booking.Status = "CONSUMED";
                    booking.UpdatedAt = DateTime.UtcNow;
                    await bookingRepo.UpdateAsync(booking, ct);
                }

                await repo.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // va đập unique index: có phiên khác vừa chiếm dock
                throw new InvalidOperationException("This charger is currently in use.", ex);
            }
            var targetSoc = dto.TargetSoc is null ? 100 : dto.TargetSoc;

            // 4) Trả DTO (giữ đúng tên InitialSoc như code cũ)
            return new ChargingSessionDto
            {
                Id = session.Id,
                Status = session.Status,
                StartedAt = session.StartedAt,
                ChargerId = session.ChargerId,
                StationId = stationId,
                ConnectorTypeId = connectorTypeId,
                BookingId = session.BookingId,

                InitialSoc = session.SocStart,

                // === BỔ SUNG ===
                VehicleBatteryCapacityKwh = vehicleModel.BatteryCapacityKwh, // decimal(12,4)
                VehicleMaxPowerKw = vehicleModel.MaxPowerKw,          // int
                ChargerPowerKw = charger.PowerKw,                  // decimal
                ConnectorMaxPowerKw = connectorType.MaxPowerKw,        // int
                TargetSoc = targetSoc
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
