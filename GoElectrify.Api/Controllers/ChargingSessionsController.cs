using GoElectrify.Api.Auth;
using GoElectrify.Api.Realtime;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.ChargingSession;
using GoElectrify.BLL.Dtos.ChargingSession;
using GoElectrify.BLL.Dtos.Dock;
using GoElectrify.BLL.Entities;
using GoElectrify.BLL.Exceptions;
using GoElectrify.DAL.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

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
        private readonly IRealtimeTokenIssuer _tokenIssuer;
        private static readonly JsonSerializerOptions Camel = new(JsonSerializerDefaults.Web);
        public ChargingSessionsController(IChargingSessionService svc, IAblyService ably, IChargingPaymentService paymentSvc, AppDbContext db, ILogger<ChargingSessionsController> logger, IRealtimeTokenIssuer tokenIssuer)
        {
            _svc = svc;
            _ably = ably;
            _paymentSvc = paymentSvc;
            _db = db;
            _logger = logger;
            _tokenIssuer = tokenIssuer;
        }
        [Authorize(Policy = "NoUnpaidSessions")]
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
            var fromUtc = (s.StartedAt == default) ? DateTime.UtcNow.AddHours(-4) : s.StartedAt;
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
        [HttpPost("/api/v1/sessions/start")]
        public async Task<IResult> StartSession(
                                        [FromBody] BLL.Dtos.Dock.StartSessionRequest req,
                                        CancellationToken ct)
        {
            // 1) xác thực dock-jwt thuộc đúng session
            if (!JwtMatchesSession(User, req.SessionId))
                return Results.Json(new { ok = false, error = "forbidden" }, options: Camel, statusCode: 403);

            // 2) lấy session còn hiệu lực
            var s = await _db.ChargingSessions
                .FirstOrDefaultAsync(x => x.Id == req.SessionId && x.EndedAt == null, ct);
            if (s is null)
                return Results.Json(new { ok = false, error = "session_not_found_or_ended" }, options: Camel, statusCode: 404);

            // (A) BẮT BUỘC CÓ BOOKING
            if (s.BookingId is null)
                return Results.Json(new { ok = false, error = "booking_required" }, options: Camel, statusCode: 400);

            var bk = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == s.BookingId, ct);
            if (bk is null)
                return Results.Json(new { ok = false, error = "booking_not_found" }, options: Camel, statusCode: 400);

            if (!string.Equals(bk.Status, "CONFIRMED", StringComparison.OrdinalIgnoreCase))
                return Results.Json(new { ok = false, error = "booking_invalid_status", status = bk.Status }, options: Camel, statusCode: 400);

            // (B) Kiểm tra trạm & đầu nối
            var ch = await _db.Chargers
                .Select(c => new { c.Id, c.StationId, c.ConnectorTypeId })
                .FirstOrDefaultAsync(c => c.Id == s.ChargerId, ct);

            if (ch is null)
                return Results.Json(new { ok = false, error = "charger_not_found" }, options: Camel, statusCode: 400);

            if (bk.StationId != ch.StationId)
                return Results.Json(new { ok = false, error = "booking_wrong_station" }, options: Camel, statusCode: 409);

            if (bk.ConnectorTypeId != ch.ConnectorTypeId)
                return Results.Json(new { ok = false, error = "booking_wrong_connector" }, options: Camel, statusCode: 409);

            // (C) VehicleModel ⇄ ConnectorType
            var vmOk = await _db.VehicleModelConnectorTypes
                .AnyAsync(x => x.VehicleModelId == bk.VehicleModelId && x.ConnectorTypeId == bk.ConnectorTypeId, ct);
            if (!vmOk)
                return Results.Json(new { ok = false, error = "vehicle_connector_incompatible" }, options: Camel, statusCode: 422);

            // (D) Chặn start nếu chủ booking đang có session UNPAID
            var hasUnpaid = await _db.ChargingSessions
                .Where(cs => cs.Status == "UNPAID" && cs.BookingId != null)
                .Join(_db.Bookings, cs => cs.BookingId, b => b.Id, (cs, b) => new { cs, b })
                .AnyAsync(x => x.b.UserId == bk.UserId && x.cs.Id != s.Id, ct);

            if (hasUnpaid)
                return Results.Json(
                    new { ok = false, error = "user_has_unpaid_sessions", errorMsg = "Bạn đang có phiên sạc chưa thanh toán" },
                    options: Camel,
                    statusCode: 409
                );

            // 3) dockId trong token phải khớp trụ của session
            if (!int.TryParse(User.FindFirst("dockId")?.Value, out var dockIdFromToken) || dockIdFromToken != s.ChargerId)
                return Results.Json(new { ok = false, error = "forbidden" }, options: Camel, statusCode: 403);

            // --- phần code có sẵn phía dưới vẫn giữ nguyên ---
            s.Status = "RUNNING";
            s.StartedAt = DateTime.UtcNow;
            if (req.TargetSoc.HasValue)
                s.TargetSoc = Math.Clamp(req.TargetSoc.Value, 10, 100);

            await _db.SaveChangesAsync(ct);

            // 5) realtime theo spec: session_started (tên event snake_case là ok)
            if (!string.IsNullOrWhiteSpace(s.AblyChannel))
            {
                await _ably.PublishAsync(
                    s.AblyChannel,
                    "session_started",
                    new { sessionId = s.Id, targetSoc = s.TargetSoc }, // camelCase field
                    ct);
            }

            // 6) trả về camelCase
            return Results.Json(new
            {
                ok = true,
                data = new
                {
                    id = s.Id,
                    status = s.Status,
                    startedAt = s.StartedAt,
                    targetSoc = s.TargetSoc,
                    socStart = s.SocStart,
                    bookingId = s.BookingId,
                    chargerId = s.ChargerId
                }
            }, options: Camel);
        }


        [Authorize(AuthenticationSchemes = "DockJwt", Policy = "DockSessionWrite")]
        [HttpPost("/api/v1/sessions/{id:int}/complete")]
        public async Task<IResult> CompleteSession([FromRoute] int id,
        [FromBody] CompleteSessionRequest req, CancellationToken ct)
        {
            // 1) Auth theo session
            if (!JwtMatchesSession(User, id))
                return Results.Json(new { ok = false, error = "forbidden" }, options: Camel, statusCode: 403);

            // 2) Lấy session còn hiệu lực
            var s = await _db.ChargingSessions.FirstOrDefaultAsync(x => x.Id == id && x.EndedAt == null, ct);
            if (s is null)
                return Results.Json(new { ok = false, error = "session_not_found_or_already_ended" }, options: Camel, statusCode: 404);

            // 3) dockId phải khớp charger
            var claimDockId = int.TryParse(User.FindFirst("dockId")?.Value, out var did) ? did : (int?)null;
            if (claimDockId is null || claimDockId.Value != s.ChargerId)
                return Results.Json(new { ok = false, error = "forbidden" }, options: Camel, statusCode: 403);

            // 4) Chốt phiên
            s.EndedAt = DateTime.UtcNow;
            var totalMinutes = (s.EndedAt.Value - s.StartedAt).TotalMinutes;
            s.DurationMinutes = Math.Max(0, (int)Math.Round(totalMinutes, MidpointRounding.AwayFromZero));
            s.FinalSoc = req.EndSoc;

            // 5) Tính Cost nếu có giá (KHÔNG thêm field mới)
            var price = await _db.Chargers
                .Where(c => c.Id == s.ChargerId)
                .Select(c => c.PricePerKwh)
                .FirstOrDefaultAsync(ct);

            if (price.HasValue)
                s.Cost = Math.Round(s.EnergyKwh * price.Value, 2, MidpointRounding.AwayFromZero);
            else
                s.Cost = null; // giữ null nếu bạn chưa cấu hình giá

            // 6) Đặt trạng thái -> UNPAID
            s.Status = "UNPAID";

            await _db.SaveChangesAsync(ct);

            return Results.Json(new
            {
                ok = true,
                data = new
                {
                    id = s.Id,
                    status = s.Status,
                    energyKwh = s.EnergyKwh,
                    cost = s.Cost,
                    endedAt = s.EndedAt
                }
            }, options: Camel);
        }


        public sealed record BindBookingRequest(int? BookingId, string? BookingCode, int? InitialSoc, int? TargetSoc);

        [Authorize] // user JWT
        [HttpPost("{id:int}/bind-booking")]
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
        [Authorize] // User JWT
        [HttpPost("{id:int}/pay")]
        public async Task<IActionResult> PayUnpaid([FromRoute] int id, CancellationToken ct)
        {
            var userId = User.GetUserId();

            // Lấy session UNPAID thuộc về user THÔNG QUA Booking
            var s = await _db.ChargingSessions
                .Include(x => x.Booking)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.Status == "UNPAID" &&
                    x.BookingId != null &&
                    x.Booking!.UserId == userId, ct);

            if (s is null)
                return NotFound(new { ok = false, error = "not_found_or_not_unpaid" });

            // Idempotent: đã thanh toán rồi
            if (string.Equals(s.Status, "PAID", StringComparison.OrdinalIgnoreCase))
            {
                var myWallet = await _db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId, ct);
                return Ok(new { ok = true, data = new { sessionId = s.Id, status = s.Status, walletBalance = myWallet?.Balance } });
            }

            // Số tiền cần trả
            var amount = s.Cost ?? 0m;

            // Nếu chưa có giá (cost=null) ⇒ coi như 0đ, chuyển PAID
            if (s.Cost is null)
            {
                s.Status = "PAID";
                await _db.SaveChangesAsync(ct);
                var myWallet = await _db.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId, ct);
                return Ok(new { ok = true, data = new { sessionId = s.Id, status = s.Status, paid = 0m, walletBalance = myWallet?.Balance } });
            }

            // Lấy ví user
            var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, ct);
            if (wallet is null)
                return BadRequest(new { ok = false, error = "no_wallet" });

            // Check số dư
            if (wallet.Balance < amount)
                return BadRequest(new { ok = false, error = "insufficient_funds", errorMsg = "Không đủ số dư" });

            // Trừ tiền + tạo Transaction (Type/Status theo schema hiện có)
            using var tx = await _db.Database.BeginTransactionAsync(ct);

            // Reload để tránh race
            await _db.Entry(wallet).ReloadAsync(ct);
            if (wallet.Balance < amount)
                return BadRequest(new { ok = false, error = "insufficient_funds", errorMsg = "Không đủ số dư" });

            wallet.Balance -= amount;

            _db.Transactions.Add(new Transaction
            {
                WalletId = wallet.Id,
                ChargingSessionId = s.Id,
                Amount = -amount,
                Type = "CHARGING",
                Status = "SUCCEEDED",
                Note = $"Pay session #{s.Id}"
            });

            s.Status = "PAID";

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Ok(new
            {
                ok = true,
                data = new { sessionId = s.Id, status = s.Status, paid = amount, walletBalance = wallet.Balance }
            });
        }

        [Authorize]
        [HttpGet("me/current")]
        public async Task<IResult> GetMyCurrent([FromQuery] bool includeUnpaid = false, CancellationToken ct = default)
        {
            var userId = User.GetUserId();

            // 1) phiên đang mở (EndedAt == null)
            var active = await _db.ChargingSessions
                .Where(s => s.EndedAt == null && s.BookingId != null)
                .Join(_db.Bookings, s => s.BookingId, b => b.Id, (s, b) => new { s, b })
                .Where(x => x.b.UserId == userId)
                .OrderByDescending(x => x.s.StartedAt == default ? DateTime.MinValue : x.s.StartedAt)
                .Select(x => new
                {
                    id = x.s.Id,
                    status = x.s.Status,
                    startedAt = x.s.StartedAt,
                    endedAt = x.s.EndedAt,
                    targetSoc = x.s.TargetSoc,
                    socStart = x.s.SocStart,
                    finalSoc = x.s.FinalSoc,
                    energyKwh = x.s.EnergyKwh,
                    cost = x.s.Cost,
                    bookingId = x.s.BookingId,
                    chargerId = x.s.ChargerId,
                    ablyChannel = x.s.AblyChannel
                })
                .FirstOrDefaultAsync(ct);

            if (active is not null)
                return Results.Json(new { ok = true, data = new { type = "active", session = active } }, options: Camel);

            // 2) nếu không có phiên mở, có thể trả phiên UNPAID gần nhất (khi includeUnpaid=true)
            if (includeUnpaid)
            {
                var unpaid = await _db.ChargingSessions
                    .Where(s => s.Status == "UNPAID" && s.BookingId != null)
                    .Join(_db.Bookings, s => s.BookingId, b => b.Id, (s, b) => new { s, b })
                    .Where(x => x.b.UserId == userId)
                    .OrderByDescending(x => x.s.EndedAt ?? DateTime.MinValue)
                    .Select(x => new
                    {
                        id = x.s.Id,
                        status = x.s.Status,
                        startedAt = x.s.StartedAt,
                        endedAt = x.s.EndedAt,
                        targetSoc = x.s.TargetSoc,
                        socStart = x.s.SocStart,
                        finalSoc = x.s.FinalSoc,
                        energyKwh = x.s.EnergyKwh,
                        cost = x.s.Cost,
                        bookingId = x.s.BookingId,
                        chargerId = x.s.ChargerId,
                        ablyChannel = x.s.AblyChannel
                    })
                    .FirstOrDefaultAsync(ct);

                if (unpaid is not null)
                    return Results.Json(new { ok = true, data = new { type = "unpaid", session = unpaid } }, options: Camel);
            }

            return Results.Json(new { ok = false, error = "no_current_session" }, options: Camel, statusCode: 404);
        }

        [Authorize] // KHÔNG áp NoUnpaidSessions
        [HttpGet("me/history")]
        public async Task<IResult> GetMyHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string? status = null,
            CancellationToken ct = default)
        {
            var userId = User.GetUserId();
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var statuses = (status ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToUpperInvariant())
                .ToHashSet();

            // chỉ lấy các phiên đã kết thúc (EndedAt != null)
            var q = _db.ChargingSessions
                .Where(s => s.EndedAt != null && s.BookingId != null)
                .Join(_db.Bookings, s => s.BookingId, b => b.Id, (s, b) => new { s, b })
                .Where(x => x.b.UserId == userId);

            if (from.HasValue) q = q.Where(x => x.s.EndedAt >= from.Value);
            if (to.HasValue) q = q.Where(x => x.s.EndedAt < to.Value);
            if (statuses.Count > 0) q = q.Where(x => x.s.Status != null && statuses.Contains(x.s.Status.ToUpper()));

            var total = await q.CountAsync(ct);

            var items = await q
                .OrderByDescending(x => x.s.EndedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    id = x.s.Id,
                    status = x.s.Status,
                    startedAt = x.s.StartedAt,
                    endedAt = x.s.EndedAt,
                    durationMinutes = x.s.DurationMinutes,
                    targetSoc = x.s.TargetSoc,
                    socStart = x.s.SocStart,
                    finalSoc = x.s.FinalSoc,
                    energyKwh = x.s.EnergyKwh,
                    cost = x.s.Cost,
                    bookingId = x.s.BookingId,
                    chargerId = x.s.ChargerId,
                    ablyChannel = x.s.AblyChannel
                })
                .ToListAsync(ct);

            return Results.Json(new
            {
                ok = true,
                data = new
                {
                    page,
                    pageSize,
                    total,
                    items
                }
            }, options: Camel);
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
            catch (BusinessRuleException brx)
            {
                return BadRequest(new
                {
                    ok = false,
                    code = brx.Code,
                    message = brx.Message,
                    suggestion = brx.Code switch
                    {
                        "WALLET_INSUFFICIENT" => "Vui lòng thanh toán bằng gói hoặc nạp thêm tiền.",
                        "SUBSCRIPTION_INSUFFICIENT" => "Vui lòng thanh toán bằng số dư ví ảo hoặc mua thêm gói.",
                        _ => "Vui lòng thử lại."
                    }
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { ok = false, error = ex.Message });
            }
        }
        [Authorize]
        [HttpGet("me/current-with-token")]
        public async Task<IResult> GetMyCurrentWithToken(
        [FromQuery] bool includeUnpaid = true,
        CancellationToken ct = default)
        {
            var userId = User.GetUserId();

            // 1) Tìm phiên active; nếu không có và includeUnpaid=true → lấy UNPAID gần nhất
            var active = await _db.ChargingSessions
                .Where(s => s.EndedAt == null && s.BookingId != null)
                .Join(_db.Bookings, s => s.BookingId, b => b.Id, (s, b) => new { s, b })
                .Where(x => x.b.UserId == userId)
                .OrderByDescending(x => x.s.StartedAt == default ? DateTime.MinValue : x.s.StartedAt)
                .Select(x => x.s)
                .FirstOrDefaultAsync(ct);

            var s = active;
            if (s is null && includeUnpaid)
            {
                s = await _db.ChargingSessions
                    .Where(x => x.Status == "UNPAID" && x.BookingId != null)
                    .Join(_db.Bookings, s => s.BookingId, b => b.Id, (s, b) => new { s, b })
                    .Where(x => x.b.UserId == userId)
                    .OrderByDescending(x => x.s.EndedAt ?? DateTime.MinValue)
                    .Select(x => x.s)
                    .FirstOrDefaultAsync(ct);
            }

            if (s is null)
                return Results.Json(new { ok = false, error = "no_current_session" }, options: Camel, statusCode: 404);

            if (string.IsNullOrWhiteSpace(s.AblyChannel))
                return Results.Json(new { ok = false, error = "no_ably_channel" }, options: Camel, statusCode: 409);

            // 2) Cấp token realtime (subscribe-only) cho user + dùng cache nội bộ service
            var (tokenObj, exp) = await _tokenIssuer.IssueAsync(
                sessionId: s.Id,
                channelId: s.AblyChannel!,
                clientId: $"user-{userId}",
                subscribeOnly: true,
                useCache: true,
                allowPresence: true,
                allowHistory: false,
                ct: ct);

            // 3) Chuẩn bị response
            return Results.Json(new
            {
                ok = true,
                data = new
                {
                    session = new
                    {
                        id = s.Id,
                        status = s.Status,
                        startedAt = s.StartedAt,
                        endedAt = s.EndedAt,
                        targetSoc = s.TargetSoc,
                        socStart = s.SocStart,
                        finalSoc = s.FinalSoc,
                        energyKwh = s.EnergyKwh,
                        cost = s.Cost,
                        bookingId = s.BookingId,
                        chargerId = s.ChargerId,
                        channelId = s.AblyChannel
                    },
                    ablyToken = tokenObj,
                    expiresAt = exp
                }
            }, options: Camel);
        }

        [Authorize]
        [HttpPost("realtime/session-token")]
        public async Task<IResult> GetSessionToken([FromBody] JsonElement body, CancellationToken ct)
        {
            if (!body.TryGetProperty("sessionId", out var sidEl) || !sidEl.TryGetInt32(out var sessionId))
                return Results.Json(new { ok = false, error = "invalid_session_id" }, options: Camel, statusCode: 400);

            var userId = User.GetUserId();

            var s = await _db.ChargingSessions
                .Where(x => x.Id == sessionId && x.BookingId != null)
                .Join(_db.Bookings, s2 => s2.BookingId, b => b.Id, (s2, b) => new { s2, b })
                .Where(x => x.b.UserId == userId)
                .Select(x => x.s2)
                .FirstOrDefaultAsync(ct);

            if (s is null || string.IsNullOrWhiteSpace(s.AblyChannel))
                return Results.Json(new { ok = false, error = "not_found" }, options: Camel, statusCode: 404);

            var (tokenObj, exp) = await _tokenIssuer.IssueAsync(
                sessionId: s.Id,
                channelId: s.AblyChannel!,
                clientId: $"user-{userId}",
                subscribeOnly: true,
                useCache: true,
                ct: ct);

            return Results.Json(new
            {
                ok = true,
                data = new
                {
                    channelId = s.AblyChannel,
                    ablyToken = tokenObj,
                    expiresAt = exp
                }
            }, options: Camel);
        }



    }
}