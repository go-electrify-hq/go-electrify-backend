using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GoElectrify.Api.Realtime;
using GoElectrify.BLL.Dto.Charger;
using GoElectrify.BLL.Dtos.Dock;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/docks")]
    public sealed class DocksController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAblyService _ably;
        private readonly IConfiguration _cfg;
        public DocksController(AppDbContext db, IAblyService ably, IConfiguration cfg)
        {
            _db = db; _ably = ably; _cfg = cfg;
        }

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
            string? sessionChannel = null;
            if (session is not null && !string.IsNullOrWhiteSpace(session.AblyChannel))
                sessionChannel = session.AblyChannel;

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

            if (sessionChannel is not null)
            {
                await _ably.PublishAsync(sessionChannel, "telemetry", new
                {
                    currentSOC = log.SocPercent,
                    powerKw = log.PowerKw,
                    energyKwh = log.SessionEnergyKwh,
                    voltageV = log.Voltage,
                    currentA = log.Current,
                    at = log.SampleAt
                }, ct);
            }

            // 4b) Snapshot tiến độ phiên (nếu đang chạy)
            if (session is not null)
            {
                var progress = new
                {
                    sessionId = session.Id,
                    energyKwh = session.EnergyKwh,
                    soc = session.SocEnd ?? session.SocStart,
                    at = log.SampleAt
                };

                if (sessionChannel is not null)
                    await _ably.PublishAsync(sessionChannel, "session.progress", progress, ct);

                // (tuỳ) vẫn bắn về kênh dock để không phá FE cũ
                await _ably.PublishAsync(channel, "session.progress", progress, ct);
            }

            return Ok(new { ok = true });
        }
        public sealed record DockHandshakeRequest(string SecretKey);

        [HttpPost("{dockId:int}/handshake")]
        public async Task<IActionResult> Handshake([FromRoute] int dockId,
                                                   [FromBody] DockHandshakeRequest body,
                                                   [FromServices] IAblyService ably,
                                                   CancellationToken ct)
        {
            // 1) Tìm Charger (Dock)
            var charger = await _db.Chargers.FirstOrDefaultAsync(c => c.Id == dockId, ct);
            if (charger is null) return NotFound(new { ok = false, error = "Dock not found." });

            // 2) Verify secret
            if (!VerifySecret(charger.DockSecretHash, body.SecretKey))
                return Unauthorized(new { ok = false, error = "Invalid secret." });

            // 3) Idempotent: nếu đã có phiên active (EndedAt == null), dùng lại
            var session = await _db.ChargingSessions
                .Where(s => s.ChargerId == charger.Id && s.EndedAt == null)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(ct);

            if (session is null)
            {
                // 3a) Tạo session PENDING
                session = new ChargingSession
                {
                    ChargerId = charger.Id,
                    Status = "PENDING",
                    // StartedAt để trống, chỉ set khi Start
                    //StartedAt = DateTime.UtcNow, 
                    SocStart = 0,
                    DurationMinutes = 0
                };
                _db.ChargingSessions.Add(session);
                await _db.SaveChangesAsync(ct); // lấy SessionId
            }

            // 4) Gán AblyChannel & JoinCode nếu chưa có
            if (string.IsNullOrWhiteSpace(session.AblyChannel))
                session.AblyChannel = $"ge:session:{session.Id}";

            if (string.IsNullOrWhiteSpace(session.JoinCode))
                session.JoinCode = await GenerateUniqueJoinCodeAsync(_db, length: 6, ct);

            await _db.SaveChangesAsync(ct);

            // 5) Ably token scoped theo kênh phiên
            var channel = session.AblyChannel!;
            var capability = $@"{{""{channel}"":[""publish"",""subscribe""]}}";
            var ttl = TimeSpan.FromHours(2);
            var token = await ably.CreateTokenAsync(channel, $"dock-{dockId}", capability, ttl, ct);

            // 5b) Dock JWT scoped theo session (dùng ở A3 để gọi /sessions/{id}/start|complete)
            var dockJwt = IssueDockSessionJwt(dockId, session.Id, ttl);

            // 6) Cập nhật trạng thái Dock (giữ logic cũ)
            charger.AblyChannel = charger.AblyChannel ?? $"ge:dock:{dockId}";
            charger.DockStatus = "CONNECTED";
            charger.LastConnectedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return Ok(new
            {
                ok = true,
                data = new
                {
                    sessionId = session.Id,
                    channelId = session.AblyChannel,
                    joinCode = session.JoinCode,
                    ablyToken = token,
                    dockJwt,                           // <-- NEW
                    expiresAt = DateTime.UtcNow.Add(ttl)
                }
            });
        }

        // Helper: tạo join code không trùng
        private static async Task<string> GenerateUniqueJoinCodeAsync(AppDbContext db, int length, CancellationToken ct)
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // bỏ O/0/1/I để dễ đọc
            var rnd = Random.Shared;

            while (true)
            {
                var chars = Enumerable.Range(0, length).Select(_ => alphabet[rnd.Next(alphabet.Length)]);
                var code = new string(chars.ToArray());

                var exists = await db.ChargingSessions.AnyAsync(s => s.JoinCode == code, ct);
                if (!exists) return code;
            }
        }


        private static bool VerifySecret(string? storedHash, string inputSecret)
        {
            if (string.IsNullOrWhiteSpace(storedHash)) return false;

            var parts = storedHash.Split('.', 2);
            if (parts.Length != 2) return false;

            byte[] salt, expectedHash;
            try
            {
                salt = Convert.FromHexString(parts[0]);
                expectedHash = Convert.FromHexString(parts[1]);
            }
            catch (FormatException)
            {
                return false; // salt/hash không phải hex hợp lệ
            }

            var secretBytes = Encoding.UTF8.GetBytes(inputSecret);

            // ghép salt || secret
            var data = new byte[salt.Length + secretBytes.Length];
            Buffer.BlockCopy(salt, 0, data, 0, salt.Length);
            Buffer.BlockCopy(secretBytes, 0, data, salt.Length, secretBytes.Length);

            using var sha = SHA256.Create();
            var actualHash = sha.ComputeHash(data);

            // so sánh constant-time trên MẢNG BYTE
            return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
        }


        public sealed record JoinByCodeRequest(string Code, string Role = "dashboard");

        [AllowAnonymous] // hoặc [Authorize] tuỳ nhu cầu
        [HttpPost("join")]
        public async Task<IActionResult> JoinByCode([FromBody] JoinByCodeRequest body,
                                                    [FromServices] IAblyService ably,
                                                    CancellationToken ct)
        {
            var s = await _db.ChargingSessions
                .FirstOrDefaultAsync(x => x.JoinCode == body.Code && x.EndedAt == null, ct);
            if (s is null) return NotFound(new { ok = false, error = "Invalid or expired code." });

            var channel = s.AblyChannel!;
            var canPublish = body.Role == "dashboard"; // dashboard có thể publish start_session
            var cap = canPublish
                ? $@"{{""{channel}"":[""subscribe"",""publish""]}}"
                : $@"{{""{channel}"":[""subscribe""]}}";
            var ttl = TimeSpan.FromHours(1);

            var clientId = $"{body.Role}-{Guid.NewGuid():N}";
            var token = await ably.CreateTokenAsync(channel, clientId, cap, ttl, ct);

            return Ok(new
            {
                ok = true,
                data = new { sessionId = s.Id, channelId = channel, token, expiresAt = DateTime.UtcNow.Add(ttl) }
            });
        }
        private string IssueDockSessionJwt(int dockId, int sessionId, TimeSpan ttl)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_cfg["DockAuth:SigningKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("role", "Dock"),
                new Claim("dockId", dockId.ToString()),
                new Claim("sessionId", sessionId.ToString()),
            };

            var token = new JwtSecurityToken(
                issuer: _cfg["DockAuth:Issuer"],
                audience: _cfg["DockAuth:Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.Add(ttl),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private static bool JwtMatchesSession(ClaimsPrincipal user, int sessionId)
            => int.TryParse(user.FindFirst("sessionId")?.Value, out var sid) && sid == sessionId;

        [Authorize(AuthenticationSchemes = "DockJwt", Policy = "DockSessionWrite")]
        [HttpPost("/api/v1/sessions/{id:int}/start")]
        public async Task<IActionResult> StartSession([FromRoute] int id,
                                             [FromBody] StartSessionRequest req,
                                             CancellationToken ct)
        {
            if (!JwtMatchesSession(User, id)) return Forbid();

            var s = await _db.ChargingSessions.FirstOrDefaultAsync(x => x.Id == id && x.EndedAt == null, ct);
            var claimDockId = int.TryParse(User.FindFirst("dockId")?.Value, out var did) ? did : (int?)null;
            if (claimDockId is null || claimDockId.Value != s.ChargerId)
                return Forbid();
            if (s is null) return NotFound(new { ok = false, error = "Session not found or ended." });

            s.Status = "RUNNING";
            // Nếu Handshake trước đó đã set StartedAt, ta vẫn “chuẩn hoá” lại tại thời điểm start:
            s.StartedAt = DateTime.UtcNow;
            if (req.TargetSoc.HasValue) s.TargetSoc = req.TargetSoc.Value;

            await _db.SaveChangesAsync(ct);

            // (tuỳ chọn) bắn realtime trên kênh phiên
            if (!string.IsNullOrWhiteSpace(s.AblyChannel))
                await _ably.PublishAsync(s.AblyChannel, "session.started", new { sessionId = s.Id, targetSOC = s.TargetSoc }, ct);

            return Ok(new { ok = true, data = new { s.Id, s.Status, s.StartedAt, s.TargetSoc } });
        }

        [Authorize(AuthenticationSchemes = "DockJwt", Policy = "DockSessionWrite")]
        [HttpPost("/api/v1/sessions/{id:int}/complete")]
        public async Task<IActionResult> CompleteSession([FromRoute] int id,
                                                [FromBody] CompleteSessionRequest req,
                                                CancellationToken ct)
        {
            if (!JwtMatchesSession(User, id)) return Forbid();

            var s = await _db.ChargingSessions.FirstOrDefaultAsync(x => x.Id == id && x.EndedAt == null, ct);
            var claimDockId = int.TryParse(User.FindFirst("dockId")?.Value, out var did) ? did : (int?)null;
            if (claimDockId is null || claimDockId.Value != s.ChargerId)
                return Forbid();
            if (s is null) return NotFound(new { ok = false, error = "Session not found or already ended." });

            s.Status = "COMPLETED";
            s.FinalSoc = Math.Clamp(req.FinalSoc, 0, 100);
            s.EndedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            if (!string.IsNullOrWhiteSpace(s.AblyChannel))
                await _ably.PublishAsync(s.AblyChannel, "session.completed", new { sessionId = s.Id, finalSOC = s.FinalSoc }, ct);

            return Ok(new { ok = true, data = new { s.Id, s.Status, s.FinalSoc, s.EndedAt } });
        }

    }
}
