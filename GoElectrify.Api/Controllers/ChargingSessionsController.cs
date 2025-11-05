using GoElectrify.Api.Auth;
using GoElectrify.Api.Realtime;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.ChargingSession;
using GoElectrify.BLL.Dtos.ChargingSession;
using GoElectrify.BLL.Entities;
using GoElectrify.BLL.Exceptions;
using GoElectrify.DAL.Persistence;
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

        /// <summary>
        /// Trả về các bản ghi ChargerLog thuộc cùng Charger và nằm trong khoảng thời gian của phiên sạc.
        /// Mặc định lấy tối đa 'last' bản ghi mới nhất rồi trả về theo thời gian tăng dần (dễ vẽ chart).
        /// </summary>
        /// <param name="id">Id phiên sạc</param>
        /// <param name="last">Số bản ghi tối đa (50..2000), mặc định 200</param>
        [HttpGet("{id:int}/logs")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLogs([FromRoute] int id, [FromQuery] int last = 200, CancellationToken ct = default)
        {
            var (ok, err, window, items) = await _svc.GetLogsAsync(id, last, ct);
            if (!ok) return NotFound(new { ok = false, error = err });
            return Ok(new { ok = true, window, data = items });
        }

        private static bool JwtMatchesSession(ClaimsPrincipal user, int sessionId)
            => int.TryParse(user.FindFirst("sessionId")?.Value, out var sid) && sid == sessionId;

        [Authorize(AuthenticationSchemes = "DockJwt", Policy = "DockSessionWrite")]
        [HttpPost("/api/v1/sessions/start")]
        public async Task<IResult> StartSession(
                                        [FromBody] BLL.Dtos.Dock.StartSessionRequest req,
                                        CancellationToken ct)
        {
            // Auth theo session
            if (!JwtMatchesSession(User, req.SessionId))
                return Results.Json(new { ok = false, error = "forbidden" }, options: Camel, statusCode: 403);

            if (!int.TryParse(User.FindFirst("dockId")?.Value, out var dockIdFromToken))
                return Results.Json(new { ok = false, error = "forbidden" }, options: Camel, statusCode: 403);

            var (ok, err, data, payload) = await _svc.StartAsync(req.SessionId, dockIdFromToken, req, ct);
            if (!ok) return Results.Json(new { ok = false, error = err }, options: Camel, statusCode: 400);

            // Publish event
            var channel = await GetChannelAsync(data!.Id, ct);
            if (!string.IsNullOrWhiteSpace(channel) && payload is not null)
                await _ably.PublishAsync(channel!, "session_started", payload, ct);

            return Results.Json(new { ok = true, data }, options: Camel);
        }



        [Authorize] // user JWT
        [HttpPost("{id:int}/bind-booking")]
        public async Task<IActionResult> BindBooking([FromRoute] int id,
                                             [FromBody] BindBookingRequest body,
                                             CancellationToken ct)
        {
            var userId = User.GetUserId();

            var (ok, err, data, specsPayload) = await _svc.BindBookingAsync(userId, id, body, ct);
            if (!ok) return BadRequest(new { ok = false, error = err });

            // Publish session_specs nếu có channel
            var channel = await GetChannelAsync(data!.SessionId, ct);
            if (!string.IsNullOrWhiteSpace(channel) && specsPayload is not null)
            {
                await _ably.PublishAsync(channel!, "session_specs", specsPayload, ct);
                _logger.LogInformation("[session_specs] published to {Channel} for session {SessionId}",
                                       channel, data!.SessionId);
            }

            return Ok(new { ok = true, data });
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
            var (active, unpaid) = await _svc.GetMyCurrentAsync(userId, includeUnpaid, ct);

            if (active is not null)
                return Results.Json(new { ok = true, data = new { type = "active", session = active } }, options: Camel);

            if (includeUnpaid && unpaid is not null)
                return Results.Json(new { ok = true, data = new { type = "unpaid", session = unpaid } }, options: Camel);

            return Results.Json(new { ok = false, error = "no_current_session" }, options: Camel, statusCode: 404);
        }

        [Authorize]
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

            var result = await _svc.GetMyHistoryAsync(userId,
                new HistoryQueryDto(page, pageSize, from, to, statuses), ct);

            return Results.Json(new { ok = true, data = result }, options: Camel);
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
            var (active, unpaid) = await _svc.GetMyCurrentAsync(userId, includeUnpaid, ct);
            var s = active ?? unpaid;

            if (s is null)
                return Results.Json(new { ok = false, error = "no_current_session" }, options: Camel, statusCode: 404);

            if (string.IsNullOrWhiteSpace(s.AblyChannel))
                return Results.Json(new { ok = false, error = "no_ably_channel" }, options: Camel, statusCode: 409);

            var (tokenObj, exp) = await _tokenIssuer.IssueAsync(
                sessionId: s.Id,
                channelId: s.AblyChannel!,
                clientId: $"user-{userId}",
                subscribeOnly: false,
                useCache: true,
                allowPresence: true,
                allowHistory: false,
                ct: ct);

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
                subscribeOnly: false,
                useCache: true,
                allowPresence: true,
                allowHistory: false,
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


        private async Task<string?> GetChannelAsync(int sessionId, CancellationToken ct)
        {
            return await _db.ChargingSessions
                .AsNoTracking()
                .Where(x => x.Id == sessionId)
                .Select(x => x.AblyChannel)
                .FirstOrDefaultAsync(ct);
        }
    }
}