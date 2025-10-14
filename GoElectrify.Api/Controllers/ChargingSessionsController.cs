using GoElectrify.Api.Auth;
using GoElectrify.Api.Realtime;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.ChargingSession;
using GoElectrify.DAL.Persistence;
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
        private readonly AppDbContext _db;
        private readonly ILogger<ChargingSessionsController> _logger;
        public ChargingSessionsController(IChargingSessionService svc, IAblyService ably, AppDbContext db, ILogger<ChargingSessionsController> logger)
        {
            _svc = svc;
            _ably = ably;
            _db = db;
            _logger = logger;
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] StartSessionDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var data = await _svc.StartForDriverAsync(userId, dto, ct);

            var channel = $"ge:dock:{data.ChargerId}";

            // PHÁT SỰ KIỆN BẮT ĐẦU (đã mở rộng trường)
            try
            {
                await _ably.PublishAsync(channel, "session.started", new
                {
                    sessionId = data.Id,
                    chargerId = data.ChargerId,
                    stationId = data.StationId,
                    connectorTypeId = data.ConnectorTypeId,
                    bookingId = data.BookingId,
                    startedAt = data.StartedAt,

                    // SỐ LIỆU CHO GIẢ LẬP/FE
                    socStart = data.InitialSoc,
                    vehicleBatteryKwh = data.VehicleBatteryCapacityKwh,
                    vehicleMaxPowerKw = data.VehicleMaxPowerKw,
                    chargerPowerKw = data.ChargerPowerKw,
                    connectorMaxPowerKw = data.ConnectorMaxPowerKw,
                    targetSoc = data.TargetSoc ?? 100
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Publish session.started best-effort failed. SessionId={SessionId}", data.Id);
                // không fail request – best-effort realtime
            }

            // Response giữ nguyên cấu trúc cũ (đã có các field mới trong DTO)
            return Ok(new { ok = true, data });
        }

        public sealed record StopRequest(string Reason);
        [HttpPost("{id:int}/stop")]
        public async Task<IActionResult> Stop([FromRoute] int id, [FromBody] StopRequest body, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var data = await _svc.StopAsync(userId, id, body?.Reason ?? "MANUAL", ct);
            await _ably.PublishAsync($"ge:dock:{data.ChargerId}", "session.stopped", new
            {
                sessionId = data.Id,
                stoppedAt = DateTime.UtcNow
            }, ct);
            return Ok(new { ok = true, data });
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
    }
}
