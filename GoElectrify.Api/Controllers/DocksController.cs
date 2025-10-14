using GoElectrify.Api.Realtime;
using GoElectrify.BLL.Dto.Charger;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/docks")]
    public sealed class DocksController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAblyService _ably;
        public DocksController(AppDbContext db, IAblyService ably) { _db = db; _ably = ably; }

        public sealed record DockConnectRequest(int DockId, string SecretKey, string? FirmwareVersion);
        public sealed record DockConnectResponse(string ChannelId, string AblyToken, DateTime ExpiresAt);

        [HttpPost("connect")]
        public async Task<IActionResult> Connect([FromBody] DockConnectRequest req, CancellationToken ct)
        {
            var charger = await _db.Chargers.FirstOrDefaultAsync(c => c.Id == req.DockId, ct);
            if (charger is null) return Unauthorized(new { ok = false, error = "Dock not found" });

            if (!VerifySecret(charger.DockSecretHash, req.SecretKey))
                return Unauthorized(new { ok = false, error = "Invalid secret" });

            var channel = charger.AblyChannel ?? $"ge:dock:{req.DockId}";
            var cap = $@"{{""{channel}"":[""publish"",""subscribe""]}}";
            var ttl = TimeSpan.FromHours(2);
            var token = await _ably.CreateTokenAsync(channel, $"dock-{req.DockId}", cap, ttl, ct);

            charger.AblyChannel = channel;
            charger.DockStatus = "CONNECTED";
            charger.LastConnectedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Ok(new { ok = true, data = new DockConnectResponse(channel, token, DateTime.UtcNow.Add(ttl)) });
        }

        [HttpPost("log")]
        public async Task<IActionResult> IngestLog([FromBody] DockLogRequest req, CancellationToken ct)
        {
            // 1) Auth dock bằng secret
            var charger = await _db.Chargers.FirstOrDefaultAsync(c => c.Id == req.DockId, ct);
            if (charger is null)
                return NotFound(new { ok = false, error = "Charger not found." });
            if (!VerifySecret(charger.DockSecretHash, req.SecretKey))
                return Unauthorized(new { ok = false, error = "Invalid secret." });

            // 2) Ghi log (idempotent theo (ChargerId, SampleAt))
            var atUtc = (req.SampleAt == default ? DateTimeOffset.UtcNow : req.SampleAt).UtcDateTime;
            var log = new ChargerLog
            {
                ChargerId = req.DockId,
                SampleAt = atUtc,
                Voltage = req.Voltage,
                Current = req.Current,
                PowerKw = req.PowerKw,
                SessionEnergyKwh = req.SessionEnergyKwh,
                SocPercent = req.SocPercent,
                State = req.State,
                ErrorCode = req.ErrorCode
            };

            try
            {
                _db.ChargerLogs.Add(log);
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // duplicate -> bỏ qua
            }

            // 3) Lấy phiên đang chạy (nếu có) rồi cập nhật kWh/SOC
            var session = await _db.ChargingSessions
                .Where(s => s.ChargerId == req.DockId && s.EndedAt == null)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(ct);

            if (session is not null)
            {
                bool changed = false;

                // 3a) Ưu tiên đồng hồ cộng dồn (cẩn thận out-of-order)
                if (req.SessionEnergyKwh is not null)
                {
                    var newTotal = Math.Max(session.EnergyKwh, req.SessionEnergyKwh.Value);
                    if (newTotal != session.EnergyKwh)
                    {
                        session.EnergyKwh = Math.Round(newTotal, 4, MidpointRounding.AwayFromZero);
                        changed = true;
                    }
                }
                else if (req.PowerKw is not null)
                {
                    // 3b) Không có kWh cộng dồn: trapezoid giữa 2 mẫu có power
                    var prev = await _db.ChargerLogs
                        .Where(l => l.ChargerId == req.DockId && l.SampleAt < log.SampleAt && l.SampleAt >= session.StartedAt && l.PowerKw != null)
                        .OrderByDescending(l => l.SampleAt)
                        .FirstOrDefaultAsync(ct);

                    if (prev?.PowerKw is not null)
                    {
                        var dtHours = (decimal)(log.SampleAt - prev.SampleAt).TotalSeconds / 3600.0m;
                        if (dtHours > 0 && dtHours < 1) // chặn outlier
                        {
                            var kwhDelta = ((prev.PowerKw.Value + req.PowerKw.Value) / 2.0m) * dtHours;
                            if (kwhDelta > 0 && kwhDelta < 1000)
                            {
                                session.EnergyKwh = Math.Round(session.EnergyKwh + kwhDelta, 4, MidpointRounding.AwayFromZero);
                                changed = true;
                            }
                        }
                    }
                }

                if (req.SocPercent is not null)
                {
                    var endSoc = Math.Clamp(req.SocPercent.Value, 0, 100);
                    if (session.SocEnd != endSoc)
                    {
                        session.SocEnd = endSoc;
                        changed = true;
                    }
                }

                if (changed)
                    await _db.SaveChangesAsync(ct);
            }

            // 4) Fan-out realtime sau khi DB đã cập nhật
            var channel = charger.AblyChannel ?? $"ge:dock:{req.DockId}";

            // 4a) Telemetry thô để FE vẽ chart ngay
            await _ably.PublishAsync(channel, "dock.telemetry", new
            {
                chargerId = req.DockId,
                at = log.SampleAt,           // UTC
                voltage = log.Voltage,
                current = log.Current,
                powerKw = log.PowerKw,
                sessionEnergyKwh = log.SessionEnergyKwh,
                soc = log.SocPercent,
                state = log.State,
                error = log.ErrorCode
            }, ct);

            // 4b) Snapshot tiến độ phiên (nếu đang chạy)
            if (session is not null)
            {
                await _ably.PublishAsync(channel, "session.progress", new
                {
                    sessionId = session.Id,
                    energyKwh = session.EnergyKwh,
                    soc = session.SocEnd ?? session.SocStart,
                    at = log.SampleAt
                }, ct);
            }

            return Ok(new { ok = true });
        }


        private static bool VerifySecret(string? storedHash, string inputSecret)
        {
            if (string.IsNullOrWhiteSpace(storedHash))
                return false;
            var parts = storedHash.Split('.', 2);
            if (parts.Length != 2) return false;
            var salt = Convert.FromHexString(parts[0]);
            var expected = parts[1];
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash([.. salt, .. Encoding.UTF8.GetBytes(inputSecret)]);
            var actual = Convert.ToHexString(bytes).ToLowerInvariant();
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual));
        }
    }
}
