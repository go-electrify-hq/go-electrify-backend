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
            // 1) Xác thực dock theo Secret (dùng VerifySecret của bạn)
            var charger = await _db.Chargers.FirstOrDefaultAsync(c => c.Id == req.DockId, ct);
            if (charger is null)
                return NotFound(new { ok = false, error = "Charger not found." });
            if (!VerifySecret(charger.DockSecretHash, req.SecretKey))
                return Unauthorized(new { ok = false, error = "Invalid secret." });

            // 2) Ghi log (idempotent theo (ChargerId, SampleAt))
            var log = new ChargerLog
            {
                ChargerId = req.DockId,
                SampleAt = (req.SampleAt == default ? DateTimeOffset.UtcNow : req.SampleAt).UtcDateTime,
                Voltage = req.Voltage,
                Current = req.Current,
                PowerKw = req.PowerKw,
                SessionEnergyKwh = req.SessionEnergyKwh,
                SocPercent = req.SocPercent,
                State = req.State,
                ErrorCode = req.ErrorCode
            };

            // Tránh duplicate theo unique index: (ChargerId, SampleAt)
            try
            {
                _db.ChargerLogs.Add(log);
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // duplicate -> bỏ qua (idempotent)
            }

            // 3) Cập nhật phiên sạc đang active (nếu có)
            var session = await _db.ChargingSessions
                .Where(s => s.ChargerId == req.DockId && s.EndedAt == null)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(ct);

            if (session is not null)
            {
                bool changed = false;

                // 3a) Cập nhật năng lượng
                if (req.SessionEnergyKwh is not null)
                {
                    // Nếu dock gửi đồng hồ cộng dồn trong phiên → lấy max để an toàn
                    var newTotal = Math.Max(session.EnergyKwh, req.SessionEnergyKwh.Value);
                    if (newTotal != session.EnergyKwh)
                    {
                        session.EnergyKwh = Math.Round(newTotal, 4, MidpointRounding.AwayFromZero);
                        changed = true;
                    }
                }
                else if (req.PowerKw is not null)
                {
                    // Không có kWh cộng dồn: xấp xỉ tích phân P theo thời gian giữa 2 mẫu
                    var prev = await _db.ChargerLogs
                        .Where(l => l.ChargerId == req.DockId && l.SampleAt < log.SampleAt && l.PowerKw != null)
                        .OrderByDescending(l => l.SampleAt)
                        .FirstOrDefaultAsync(ct);

                    if (prev is not null && prev.PowerKw is not null)
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

                // 3b) Cập nhật SoC cuối
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
