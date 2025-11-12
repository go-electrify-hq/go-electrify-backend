using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.ChargingSession;
using GoElectrify.BLL.Dtos.ChargingSession;
using GoElectrify.BLL.Dtos.Dock;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace GoElectrify.BLL.Services
{
    // Thêm các repo cần thiết vào primary-constructor
    public sealed class ChargingSessionService(
        IChargingSessionRepository repo,
        IBookingRepository bookingRepo,
        IChargerRepository chargerRepo,
        IChargerLogRepository logRepo,
        IStationRepository stationRepo,
        INotificationMailService _notifMail
    ) : IChargingSessionService
    {
        public async Task<ChargingSessionDto> StopAsync(int userId, int sessionId, string reason, CancellationToken ct)
        {
            var s = await repo.GetSessionAsync(sessionId, ct) ?? throw new InvalidOperationException("Session not found.");
            if (s.EndedAt is null)
            {
                s.EndedAt = DateTime.UtcNow;
                s.Status = "TIMEOUT";
                s.DurationSeconds = (int)Math.Max(0, (s.EndedAt.Value - s.StartedAt).TotalSeconds);
                await repo.SaveChangesAsync(ct);
            }

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

        public async Task<IReadOnlyList<ChargingSessionDto>> GetByStationAsync(
            int stationId,
            StationSessionQueryDto q,
            CancellationToken ct)
        {
            DateTime? ToUtc(DateTime? d) => d.HasValue ? d.Value.ToUniversalTime() : null;

            var list = await repo.GetByStationAsync(
                stationId,
                string.IsNullOrWhiteSpace(q.Status) ? null : q.Status!.Trim().ToUpperInvariant(),
                ToUtc(q.From),
                ToUtc(q.To),
                q.Page,
                q.PageSize,
                ct);

            var result = list.Select(s => new ChargingSessionDto
            {
                Id = s.Id,
                Status = s.Status,
                StartedAt = s.StartedAt,
                ChargerId = s.ChargerId,

                StationId = s.Charger?.StationId ?? 0,
                ConnectorTypeId = s.Charger?.ConnectorTypeId ?? 0,

                BookingId = s.BookingId,
                InitialSoc = s.SocStart,

                VehicleBatteryCapacityKwh = s.Booking?.VehicleModel?.BatteryCapacityKwh ?? 0m,
                VehicleMaxPowerKw = s.Booking?.VehicleModel?.MaxPowerKw ?? 0,

                ChargerPowerKw = s.Charger?.PowerKw ?? 0m,
                ConnectorMaxPowerKw = s.Charger?.ConnectorType?.MaxPowerKw ?? 0,

                TargetSoc = s.TargetSoc
            }).ToList();

            return result;
        }

        public async Task<(bool Ok, string? Error, SessionLogWindow Window, IReadOnlyList<SessionLogItemDto> Items)>
            GetLogsAsync(int sessionId, int last, CancellationToken ct)
        {
            var s = await repo.GetSessionAsync(sessionId, ct);
            if (s is null)
                return (false, "Session not found.", default, Array.Empty<SessionLogItemDto>());

            var fromUtc = (s.StartedAt == default) ? DateTime.UtcNow.AddHours(-4) : s.StartedAt;
            var toUtc = s.EndedAt ?? DateTime.UtcNow;

            var raw = await logRepo.GetLastByChargerBetweenAsync(s.ChargerId, fromUtc, toUtc, last, ct);

            var items = raw.Select(l => new SessionLogItemDto
            {
                At = l.SampleAt,
                Voltage = l.Voltage,
                Current = l.Current,
                PowerKw = l.PowerKw,
                SessionEnergyKwh = l.SessionEnergyKwh,
                SocPercent = l.SocPercent,
                State = l.State,
                ErrorCode = l.ErrorCode
            }).ToList();

            return (true, null, new SessionLogWindow(fromUtc, toUtc), items);
        }

        public async Task<(ChargingSessionLightDto? Active, ChargingSessionLightDto? Unpaid)>
            GetMyCurrentAsync(int userId, bool includeUnpaid, CancellationToken ct)
        {
            var active = await repo.GetActiveByUserAsync(userId, ct);
            ChargingSessionLightDto? activeDto = active is null ? null : MapLight(active);

            ChargingSessionLightDto? unpaidDto = null;
            if (activeDto is null && includeUnpaid)
            {
                var unpaid = await repo.GetClosestUnpaidByUserAsync(userId, ct);
                unpaidDto = unpaid is null ? null : MapLight(unpaid);
            }

            return (activeDto, unpaidDto);
        }

        public async Task<PagedResult<ChargingSessionHistoryItemDto>>
            GetMyHistoryAsync(int userId, HistoryQueryDto q, CancellationToken ct)
        {
            var (total, items) = await repo.GetHistoryForUserAsync(
                userId, q.From, q.To, q.Statuses, q.Page, q.PageSize, ct);

            var mapped = items.Select(MapHistory).ToList();
            return new PagedResult<ChargingSessionHistoryItemDto>(q.Page, q.PageSize, total, mapped);
        }

        private static ChargingSessionLightDto MapLight(ChargingSession s) =>
            new(
                s.Id, s.Status, s.StartedAt, s.EndedAt,
                s.TargetSoc, s.SocStart, s.FinalSoc,
                s.EnergyKwh, s.Cost, s.BookingId, s.ChargerId, s.AblyChannel
            );

        private static ChargingSessionHistoryItemDto MapHistory(ChargingSession s) =>
            new(
                s.Id, s.Status, s.StartedAt, s.EndedAt, s.DurationSeconds,
                s.TargetSoc, s.SocStart, s.FinalSoc,
                s.EnergyKwh, s.Cost, s.BookingId, s.ChargerId, s.AblyChannel
            );

        public async Task<(bool Ok, string? Error, StartSessionResult? Data, object? EventPayload)>
            StartAsync(int sessionId, int dockIdFromToken, GoElectrify.BLL.Dtos.Dock.StartSessionRequest req, CancellationToken ct)
        {
            var s = await repo.GetSessionAsync(sessionId, ct);
            if (s is null || s.EndedAt != null)
                return (false, "session_not_found_or_ended", null, null);

            if (s.BookingId is null)
                return (false, "booking_required", null, null);

            var bk = await bookingRepo.GetByIdAsync(s.BookingId.Value, ct);
            if (bk is null)
                return (false, "booking_not_found", null, null);

            if (!string.Equals(bk.Status, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
                return (false, "booking_invalid_status", null, null);

            if (dockIdFromToken != s.ChargerId)
                return (false, "forbidden", null, null);

            var ch = await chargerRepo.GetByIdAsync(s.ChargerId, ct);
            if (ch is null)
                return (false, "charger_not_found", null, null);
            if (bk.StationId != ch.StationId)
                return (false, "booking_wrong_station", null, null);
            if (bk.ConnectorTypeId != ch.ConnectorTypeId)
                return (false, "booking_wrong_connector", null, null);
            if (bk.VehicleModelId.HasValue)
            {
                var compatible = await repo.VehicleSupportsConnectorAsync(bk.VehicleModelId.Value, bk.ConnectorTypeId, ct);
                if (!compatible)
                    return (false, "vehicle_connector_incompatible", null, null);
            }


            var hasUnpaid = await repo.UserHasOtherUnpaidAsync(bk.UserId, s.Id, ct);
            if (hasUnpaid)
                return (false, "user_has_unpaid_sessions", null, null);

            // Start
            s.Status = "RUNNING";
            s.StartedAt = DateTime.UtcNow;
            if (req.TargetSoc.HasValue)
                s.TargetSoc = Math.Clamp(req.TargetSoc.Value, 10, 100);
            

            await repo.SaveChangesAsync(ct);

            var data = new StartSessionResult(
                s.Id, s.Status, s.StartedAt, s.TargetSoc, s.SocStart, s.BookingId, s.ChargerId
            );

            // Payload cho Ably event "session_started"
            var payload = new
            {
                sessionId = s.Id,
                targetSoc = s.TargetSoc
            };

            return (true, null, data, payload);
        }

        public async Task<(bool Ok, string? Error, CompleteSessionResult? Data)>
            CompleteByDockAsync(int sessionId, int dockIdFromToken, CompleteSessionRequest req, CancellationToken ct)
        {
            // Lấy session đang mở
            var s = await repo.GetSessionAsync(sessionId, ct);
            if (s is null || s.EndedAt != null)
                return (false, "session_not_found_or_already_ended", null);

            // Dock phải khớp charger
            if (dockIdFromToken != s.ChargerId)
                return (false, "forbidden", null);

            // Chốt phiên
            s.EndedAt = DateTime.UtcNow;
            var started = s.StartedAt; // có thể là default(DateTime) nếu chưa start
            var seconds = (started == default)
                ? 0
                : (int)Math.Max(0, Math.Round((s.EndedAt.Value - started).TotalSeconds, MidpointRounding.AwayFromZero));

            s.DurationSeconds = seconds;
            s.FinalSoc = req.EndSoc;

            var charger = await repo.GetChargerAsync(s.ChargerId, ct);

            // Tính Cost nếu có giá
            decimal? pricePerKwh = (await repo.GetChargerAsync(s.ChargerId, ct))?.PricePerKwh;
            s.Cost = pricePerKwh.HasValue
                ? Math.Round(s.EnergyKwh * pricePerKwh.Value, 2, MidpointRounding.AwayFromZero)
                : null;

            // Trạng thái -> UNPAID (không thêm field mới)
            s.Status = "UNPAID";
            if (!string.Equals(s.Booking.Status, "CONSUMED", StringComparison.OrdinalIgnoreCase))
                s.Booking.Status = "CONSUMED";
            await repo.SaveChangesAsync(ct);

            // ================== [MAIL] gửi email "Phiên sạc hoàn tất" ==================
            if (s.EndedAt is not null)
            {
                try
                {
                    string? userEmail = null;
                    if (s.BookingId is int bid)
                    {
                        var bk = await bookingRepo.GetByIdAsync(bid, ct);
                        userEmail = bk?.User?.Email;
                    }

                    if (!string.IsNullOrWhiteSpace(userEmail))
                    {
                        string stationName = "Trạm sạc";
                        if (charger != null)
                        {
                            var name = await stationRepo.GetNameByIdAsync(charger.StationId, ct);
                            stationName = string.IsNullOrWhiteSpace(name) ? $"Trạm #{charger.StationId}" : name!;
                        }

                        await _notifMail.SendChargingCompletedAsync(
                            toEmail: userEmail!,
                            stationName: stationName,
                            energyKwh: s.EnergyKwh,
                            cost: s.Cost,
                            startedAtUtc: started == default ? s.EndedAt.Value : started,
                            endedAtUtc: s.EndedAt.Value,
                            ct: ct
                        );
                    }
                }
                catch (Exception ex)
                {
                    // nếu có ILogger, bạn có thể log lại:
                    // _logger.LogWarning(ex, "Send charging completed email failed (sessionId={Id})", s.Id);
                }
            }
            // ================== [/MAIL] ==================

            var result = new CompleteSessionResult(
                s.Id,
                s.Status!,
                s.EnergyKwh,
                s.Cost,
                s.EndedAt!.Value
            );

            return (true, null, result);
        }

        public async Task<(bool Ok, string? Error, BindBookingResult? Data, object? EventPayload)>
            BindBookingAsync(int userId, int sessionId, BindBookingRequest body, CancellationToken ct)
        {
            var s = await repo.GetSessionAsync(sessionId, ct);
            if (s is null || s.EndedAt != null)
                return (false, "Session not found or ended.", null, null);

            if (body.BookingId is null && string.IsNullOrWhiteSpace(body.BookingCode))
                return (false, "Missing bookingId/bookingCode.", null, null);

            Booking? bk = body.BookingId is int bid
                ? await bookingRepo.GetByIdAsync(bid, ct)
                : await bookingRepo.GetByCodeAsync(body.BookingCode!, ct);

            if (bk is null)
                return (false, "Booking not found.", null, null);
            if (bk.UserId != userId)
                return (false, "forbidden", null, null);

            var ch = await chargerRepo.GetByIdAsync(s.ChargerId, ct);
            if (ch is null || bk.StationId != ch.StationId)
                return (false, "Booking does not belong to this charger.", null, null);

            if (bk.ConnectorTypeId != ch.ConnectorTypeId)
                return (false, "Booking connector type does not match charger.", null, null);

            if (bk.Status is not ("CONFIRMED" or "CHECKED_IN" or "RESERVED"))
                return (false, "Booking is not active.", null, null);

            // Gán vào session
            s.BookingId = bk.Id;
            if (body.InitialSoc.HasValue)
                s.SocStart = Math.Clamp(body.InitialSoc.Value, 0, 100);
            if (body.TargetSoc.HasValue)
                s.TargetSoc = Math.Clamp(body.TargetSoc.Value, 10, 100);

            await repo.SaveChangesAsync(ct);

            var data = new BindBookingResult(s.Id, s.BookingId, bk.VehicleModelId, s.SocStart, s.TargetSoc);

            // ===== Build payload cho Ably event "session_specs" =====
            // Lấy thêm thông tin vehicle & charger
            //var vm = await repo.GetVehicleModelAsync(bk.VehicleModelId, ct); // có BatteryCapacityKwh, MaxPowerKw
            VehicleModel? vm = null;
            if (bk.VehicleModelId.HasValue)
            {
                vm = await repo.GetVehicleModelAsync(bk.VehicleModelId.Value, ct);
                if (vm is null)
                    return (false, "Vehicle model not found.", null, null);
            }
            var chargerLite = ch; // đã có ở trên (PowerKw, ConnectorTypeId)

            object? payload = null;
            //if (vm is not null && chargerLite is not null)
            //{
            //    payload = new
            //    {
            //        sessionId = s.Id,
            //        initialSoc = s.SocStart,
            //        targetSoc = s.TargetSoc,
            //        booking = new
            //        {
            //            vehicleModelId = bk.VehicleModelId,
            //            connectorTypeId = bk.ConnectorTypeId,
            //            stationId = bk.StationId,
            //            scheduledStart = bk.ScheduledStart
            //        },
            //        vehicle = new
            //        {
            //            batteryCapacityKwh = vm.BatteryCapacityKwh,
            //            maxPowerKw = vm.MaxPowerKw
            //        },
            //        charger = new
            //        {
            //            powerKw = chargerLite.PowerKw,
            //            connectorTypeId = chargerLite.ConnectorTypeId
            //        }
            //    };
            //}

            if (chargerLite is not null)
            {
                var basePayload = new
                {
                    sessionId = s.Id,
                    initialSoc = s.SocStart,
                    targetSoc = s.TargetSoc,
                    booking = new
                    {
                        vehicleModelId = bk.VehicleModelId,
                        connectorTypeId = bk.ConnectorTypeId,
                        stationId = bk.StationId,
                        scheduledStart = bk.ScheduledStart
                    },
                    charger = new
                    {
                        powerKw = chargerLite.PowerKw,
                        connectorTypeId = chargerLite.ConnectorTypeId
                    }
                };
                payload = vm is null ? basePayload :
                    new
                    {
                        basePayload.sessionId,
                        basePayload.initialSoc,
                        basePayload.targetSoc,
                        basePayload.booking,
                        basePayload.charger,
                        vehicle = new
                        {
                            batteryCapacityKwh = vm.BatteryCapacityKwh,
                            maxPowerKw = vm.MaxPowerKw
                        }
                    };
            }

            return (true, null, data, payload);
        }
    }
}
