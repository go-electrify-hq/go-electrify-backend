using System.Security.Claims;
using GoElectrify.Api.Auth;
using GoElectrify.Api.Realtime;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.ChargingSession;
using GoElectrify.BLL.Dtos.ChargingSession;
using GoElectrify.BLL.Dtos.Dock;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using GoElectrify.BLL.Dtos.ChargingSession;
using GoElectrify.BLL.Contracts.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/charging-sessions")]
    [Authorize]
    public class ChargingSessionsController : ControllerBase
    {
        private readonly IChargingSessionService _svc;
        private readonly IAblyService _ably;
        private readonly IChargingPaymentService _paymentSvc;
        private readonly AppDbContext _db;
        private readonly ILogger<ChargingSessionsController> _logger;
        public ChargingSessionsController(IChargingSessionService svc, IAblyService ably, IChargingPaymentService paymentSvc, AppDbContext db, ILogger<ChargingSessionsController> logger)
        {
            _svc = svc;
            _ably = ably;
            _paymentSvc = paymentSvc;
            _db = db;
            _logger = logger;
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] StartSessionDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var data = await _svc.StartForDriverAsync(userId, dto, ct);

            // Thử tìm session vừa tạo để lấy AblyChannel
            var sess = await _db.ChargingSessions.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == data.Id, ct);

            // Ưu tiên publish vào KÊNH PHIÊN với tên 'start_session' (Dock nghe lệnh này)
            var channel = !string.IsNullOrWhiteSpace(sess?.AblyChannel)
                ? sess!.AblyChannel
                : $"ge:dock:{data.ChargerId}"; // fallback (cũ) nếu thiếu channel

            try
            {
                // LỆNH cho Dock bắt đầu (command), giữ payload đầy đủ để Dock/FE dùng
                await _ably.PublishAsync(channel!, "start_session", new
                {
                    sessionId = data.Id,
                    chargerId = data.ChargerId,
                    stationId = data.StationId,
                    connectorTypeId = data.ConnectorTypeId,
                    bookingId = data.BookingId,
                    startedAt = data.StartedAt,

                    socStart = data.InitialSoc,
                    vehicleBatteryKwh = data.VehicleBatteryCapacityKwh,
                    vehicleMaxPowerKw = data.VehicleMaxPowerKw,
                    chargerPowerKw = data.ChargerPowerKw,
                    connectorMaxPowerKw = data.ConnectorMaxPowerKw,
                    targetSOC = data.TargetSoc ?? 100
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Publish start_session best-effort failed. SessionId={SessionId}", data.Id);
            }

            return Ok(new { ok = true, data });
        }


        [HttpPost("{id:int}/stop")]
        [Authorize(
            AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + ",DockJwt",
            Policy = "DockOrStaffOrAdmin"
        )]
        public async Task<IActionResult> Stop([FromRoute] int id, [FromBody] StopSessionDto dto, CancellationToken ct)
        {
            // Nếu là DockJwt → ràng buộc đúng phiên & đúng trụ
            var isDock = User.HasClaim("role", "Dock") || User.IsInRole("Dock");
            if (isDock)
            {
                if (!int.TryParse(User.FindFirst("sessionId")?.Value, out var sid) || sid != id)
                    return Forbid();

                if (!int.TryParse(User.FindFirst("dockId")?.Value, out var dockId))
                    return Forbid();

                var s0 = await _db.ChargingSessions.FindAsync(new object?[] { id }, ct);
                if (s0 is null || s0.EndedAt != null || s0.ChargerId != dockId)
                    return Forbid();
            }

            // Gọi service mới (đã hỗ trợ finalSoc/energyKwh)
            var res = await _svc.StopAsync(
                sessionId: id,
                reason: dto.Reason ?? (isDock ? "target_soc" : "user_request"),
                finalSoc: dto.FinalSoc,
                energyKwh: dto.EnergyKwh,
                ct
            );

            // KHÔNG publish ở controller để tránh double; để service broadcast (nếu bạn đã làm vậy)
            return Ok(new
            {
                sessionId = id,
                res.EndedAt,
                summary = new
                {
                    res.DurationMinutes,
                    res.EnergyKwh,
                    res.AvgPowerKw,
                    res.Cost
                }
            });
        }

        /// <summary>
        /// Trả về các bản ghi ChargerLog thuộc cùng Charger và nằm trong khoảng thời gian của phiên sạc.
        /// Mặc định lấy tối đa 'last' bản ghi mới nhất rồi trả về theo thời gian tăng dần (dễ vẽ chart).
        /// </summary>
        /// <param name="id">Id phiên sạc</param>
        /// <param name="last">Số bản ghi tối đa (50..2000), mặc định 200</param>
        [HttpGet("{id:long}/logs")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLogs([FromRoute] long id, [FromQuery] int last = 200, CancellationToken ct = default)
        {
            // Giới hạn để tránh query quá nặng
            last = Math.Clamp(last <= 0 ? 200 : last, 50, 2000);

            // 1) Lấy phiên sạc
            var s = await _db.ChargingSessions
                             .AsNoTracking()
                             .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (s is null)
                return NotFound(new { ok = false, error = "Session not found." });

            // 2) Xác định khoảng thời gian của phiên
            var fromUtc = s.StartedAt;               // giả định UTC
            var toUtc = s.EndedAt ?? DateTime.UtcNow;

            // 3) Lấy log của cùng Charger trong khoảng thời gian phiên
            //    - Lấy 'last' bản mới nhất rồi ORDER BY tăng dần để UI vẽ mượt.
            var items = await _db.ChargerLogs
                .AsNoTracking()
                .Where(l => l.ChargerId == s.ChargerId
                         && l.SampleAt >= fromUtc
                         && l.SampleAt <= toUtc)
                .OrderByDescending(l => l.SampleAt)
                .Take(last)
                .OrderBy(l => l.SampleAt)
                .Select(l => new SessionLogItemDto
                {
                    At = l.SampleAt,
                    Voltage = l.Voltage,
                    Current = l.Current,
                    PowerKw = l.PowerKw,
                    SessionEnergyKwh = l.SessionEnergyKwh,
                    SocPercent = l.SocPercent,
                    State = l.State,
                    ErrorCode = l.ErrorCode
                })
                .ToListAsync(ct);

            return Ok(new { ok = true, data = items });
        }

        private static bool JwtMatchesSession(ClaimsPrincipal user, int sessionId)
            => int.TryParse(user.FindFirst("sessionId")?.Value, out var sid) && sid == sessionId;

        [Authorize(AuthenticationSchemes = "DockJwt", Policy = "DockSessionWrite")]
        [HttpPost("/api/v1/sessions/{id:int}/start")]
        public async Task<IActionResult> StartSession([FromRoute] int id,
                                              [FromBody] BLL.Dtos.Dock.StartSessionRequest req,
                                              CancellationToken ct)
        {
            // 1) xác thực dock-jwt thuộc đúng session
            if (!JwtMatchesSession(User, id))
                return Forbid();

            // 2) lấy session còn hiệu lực
            var s = await _db.ChargingSessions
                .FirstOrDefaultAsync(x => x.Id == id && x.EndedAt == null, ct);
            if (s is null)
                return NotFound(new { ok = false, error = "Session not found or ended." });

            // (A) BẮT BUỘC CÓ BOOKING
            if (s.BookingId is null)
                return BadRequest(new { ok = false, error = "booking_required" });

            // Load booking & charger info (chỉ dùng field đã có)
            var bk = await _db.Bookings
                .FirstOrDefaultAsync(x => x.Id == s.BookingId, ct);
            if (bk is null)
                return BadRequest(new { ok = false, error = "booking_not_found" });

            if (bk.Status != "CONFIRMED")
                return BadRequest(new { ok = false, error = "booking_invalid_status", status = bk.Status });

            // (B) Kiểm tra trạm & đầu nối theo cấu trúc sẵn có
            var ch = await _db.Chargers
                .Select(c => new { c.Id, c.StationId, c.ConnectorTypeId })
                .FirstOrDefaultAsync(c => c.Id == s.ChargerId, ct);

            if (ch is null)
                return BadRequest(new { ok = false, error = "charger_not_found" });

            if (bk.StationId != ch.StationId)
                return BadRequest(new { ok = false, error = "booking_wrong_station" });

            if (bk.ConnectorTypeId != ch.ConnectorTypeId)
                return BadRequest(new { ok = false, error = "booking_wrong_connector" });

            // (C) Kiểm tra VehicleModel có hỗ trợ ConnectorType (bảng sẵn có VehicleModelConnectorType)
            var vmOk = await _db.VehicleModelConnectorTypes
                .AnyAsync(x => x.VehicleModelId == bk.VehicleModelId
                            && x.ConnectorTypeId == bk.ConnectorTypeId, ct);
            if (!vmOk)
                return BadRequest(new { ok = false, error = "vehicle_connector_incompatible" });

            // 3) dockId trong token phải khớp trụ của session
            if (!int.TryParse(User.FindFirst("dockId")?.Value, out var dockIdFromToken) ||
                dockIdFromToken != s.ChargerId)
                return Forbid();

            // --- phần code có sẵn phía dưới vẫn giữ nguyên ---
            s.Status = "RUNNING";
            s.StartedAt = DateTime.UtcNow;
            if (req.TargetSoc.HasValue)
                s.TargetSoc = Math.Clamp(req.TargetSoc.Value, 10, 100);

            await _db.SaveChangesAsync(ct);

            // 5) realtime theo spec: session_started (snake_case)
            if (!string.IsNullOrWhiteSpace(s.AblyChannel))
            {
                await _ably.PublishAsync(
                    s.AblyChannel,
                    "session_started",
                    new { sessionId = s.Id, targetSOC = s.TargetSoc },
                    ct);
            }

            // 6) trả về chỉ những field chắc chắn có
            return Ok(new
            {
                ok = true,
                data = new
                {
                    Id = s.Id,
                    Status = s.Status,
                    StartedAt = s.StartedAt,
                    TargetSoc = s.TargetSoc,
                    SocStart = s.SocStart,
                    BookingId = s.BookingId,
                    ChargerId = s.ChargerId
                }
            });
        }


        [Authorize(AuthenticationSchemes = "DockJwt", Policy = "DockSessionWrite")]
        [HttpPost("/api/v1/sessions/{id:int}/complete")]
        public async Task<IActionResult> CompleteSession([FromRoute] int id,
            [FromBody] CompleteSessionRequest req, CancellationToken ct)
        {
            if (!JwtMatchesSession(User, id)) return Forbid();

            var s = await _db.ChargingSessions.FirstOrDefaultAsync(x => x.Id == id && x.EndedAt == null, ct);
            if (s is null) return NotFound(new { ok = false, error = "Session not found or already ended." });

            var claimDockId = int.TryParse(User.FindFirst("dockId")?.Value, out var did) ? did : (int?)null;
            if (claimDockId is null || claimDockId.Value != s.ChargerId) return Forbid();

            s.Status = "COMPLETED";
            s.FinalSoc = Math.Clamp(req.FinalSoc, 0, 100);
            s.EndedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(s.AblyChannel))
                await _ably.PublishAsync(s.AblyChannel, "session_completed",
                    new { sessionId = s.Id, finalSOC = s.FinalSoc }, ct);

            return Ok(new { ok = true, data = new { s.Id, s.Status, s.FinalSoc, s.EndedAt } });
        }

        public sealed record BindBookingRequest(int? BookingId, string? BookingCode, int? InitialSoc, int? TargetSoc);

        [Authorize] // user JWT
        [HttpPost("/api/v1/sessions/{id:int}/bind-booking")]
        public async Task<IActionResult> BindBooking([FromRoute] int id,
                                             [FromBody] BindBookingRequest body,
                                             CancellationToken ct)
        {
            // 1) Lấy session còn hiệu lực
            var s = await _db.ChargingSessions
                .FirstOrDefaultAsync(x => x.Id == id && x.EndedAt == null, ct);
            if (s is null)
                return NotFound(new { ok = false, error = "Session not found or ended." });

            // 2) Tìm booking theo Id/Code
            if (body.BookingId is null && string.IsNullOrWhiteSpace(body.BookingCode))
                return BadRequest(new { ok = false, error = "Missing bookingId/bookingCode." });

            IQueryable<Booking> q = _db.Bookings.AsNoTracking();
            if (body.BookingId is int bid)
                q = q.Where(b => b.Id == bid);
            else
                q = q.Where(b => b.Code == body.BookingCode);

            // 3) Check quyền user sở hữu booking
            var userIdClaim = User.FindFirst("uid")?.Value ?? User.FindFirst("sub")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Forbid();

            var bk = await q.FirstOrDefaultAsync(ct);
            if (bk is null)
                return NotFound(new { ok = false, error = "Booking not found." });
            if (bk.UserId != userId)
                return Forbid();

            // 4) Kiểm tra đúng station của charger trong session
            var sessionStationId = await _db.Chargers.AsNoTracking()
                .Where(c => c.Id == s.ChargerId)
                .Select(c => c.StationId)
                .FirstOrDefaultAsync(ct);

            if (sessionStationId == default || bk.StationId != sessionStationId)
                return BadRequest(new { ok = false, error = "Booking does not belong to this charger." });

            // 5) Trạng thái booking còn hiệu lực
            if (bk.Status is not ("CONFIRMED" or "CHECKED_IN" or "RESERVED"))
                return BadRequest(new { ok = false, error = "Booking is not active." });

            // 6) Gán vào session (KHÔNG lưu VehicleModelId ở session)
            s.BookingId = bk.Id;
            if (body.InitialSoc.HasValue)
                s.SocStart = Math.Clamp(body.InitialSoc.Value, 0, 100);
            if (body.TargetSoc.HasValue)
                s.TargetSoc = Math.Clamp(body.TargetSoc.Value, 10, 100);

            await _db.SaveChangesAsync(ct);

            // 7) Publish session_specs (nếu có channel)
            if (string.IsNullOrWhiteSpace(s.AblyChannel))
            {
                _logger.LogWarning("Session {SessionId} missing AblyChannel; skip publishing session_specs.", s.Id);
            }
            else
            {
                // 7a) Load lại các dữ liệu PRIMITIVE từ DB (tránh navigation để không vòng tham chiếu)
                var bookingProj = await _db.Bookings.AsNoTracking()
                    .Where(b => b.Id == s.BookingId)
                    .Select(b => new
                    {
                        b.VehicleModelId,
                        b.ConnectorTypeId,
                        b.StationId,
                        b.ScheduledStart
                    })
                    .FirstOrDefaultAsync(ct);

                var vm = (bookingProj?.VehicleModelId != null)
                    ? await _db.VehicleModels.AsNoTracking()
                        .Where(v => v.Id == bookingProj.VehicleModelId)
                        .Select(v => new
                        {
                            v.BatteryCapacityKwh,
                            v.MaxPowerKw
                        })
                        .FirstOrDefaultAsync(ct)
                    : null;

                var chargerLite = await _db.Chargers.AsNoTracking()
                    .Where(c => c.Id == s.ChargerId)
                    .Select(c => new
                    {
                        c.PowerKw,
                        c.ConnectorTypeId
                    })
                    .FirstOrDefaultAsync(ct);

                // 7b) Kiểm tra dữ liệu tối thiểu & publish
                if (bookingProj is null)
                {
                    _logger.LogWarning("BindBooking: Booking not found for session {SessionId}", s.Id);
                }
                else if (vm is null)
                {
                    _logger.LogWarning("BindBooking: VehicleModel not found for session {SessionId} (VehicleModelId={VM})",
                                       s.Id, bookingProj.VehicleModelId);
                }
                else if (chargerLite is null)
                {
                    _logger.LogWarning("BindBooking: Charger not found for session {SessionId} (ChargerId={ChargerId})",
                                       s.Id, s.ChargerId);
                }
                else
                {
                    var specs = new
                    {
                        sessionId = s.Id,
                        initialSoc = s.SocStart,
                        targetSoc = s.TargetSoc,
                        booking = new
                        {
                            vehicleModelId = bookingProj.VehicleModelId,
                            connectorTypeId = bookingProj.ConnectorTypeId,
                            stationId = bookingProj.StationId,
                            scheduledStart = bookingProj.ScheduledStart
                        },
                        vehicle = new
                        {
                            batteryCapacityKwh = vm.BatteryCapacityKwh,
                            maxPowerKw = vm.MaxPowerKw
                        },
                        charger = new
                        {
                            powerKw = chargerLite.PowerKw,
                            connectorTypeId = chargerLite.ConnectorTypeId
                        }
                    };

                    await _ably.PublishAsync(s.AblyChannel!, "session_specs", specs, ct);
                    _logger.LogInformation("[session_specs] published to {Channel} for session {SessionId}",
                                           s.AblyChannel, s.Id);
                }
            }

            // 8) Trả về thông tin cần cho FE; vehicleModelId chỉ để tham khảo (nguồn từ booking)
            return Ok(new
            {
                ok = true,
                data = new
                {
                    s.Id,
                    s.BookingId,
                    vehicleModelId = bk.VehicleModelId,
                    s.SocStart,
                    s.TargetSoc
                }
            });
        }

        [HttpPost("{id:int}/complete-payment")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompletePayment(
    [FromRoute] int id,
    [FromBody] CompletePaymentRequest dto,
    CancellationToken ct = default)
        {
            var userId = User.GetUserId();

            try
            {
                var receipt = await _paymentSvc.CompletePaymentAsync(userId, id, dto, ct);
                return Ok(new { ok = true, data = receipt });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { ok = false, error = ex.Message });
            }
        }

    }
}
