using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GoElectrify.Api.Realtime;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Charger;
using GoElectrify.BLL.Dtos.Dock;
using GoElectrify.BLL.Dtos.ChargingSession;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.IdentityModel.Tokens;
using GoElectrify.BLL.Services.Realtime;
using System.Text.Json;


namespace GoElectrify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/docks")]
    public sealed class DocksController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAblyService _ably;
        private readonly IConfiguration _cfg;
        private readonly ILogger<DocksController> _logger;
        private readonly IChargingSessionService _svc;
        private readonly IAblyTokenCache _ablyTokenCache;
        private static readonly JsonSerializerOptions Camel = new(JsonSerializerDefaults.Web);
        public DocksController(IChargingSessionService svc, AppDbContext db, IAblyService ably, IConfiguration cfg, ILogger<DocksController> logger, IAblyTokenCache ablyTokenCache)
        {
            _db = db; _ably = ably; _cfg = cfg; _logger = logger; _svc = svc; _ablyTokenCache = ablyTokenCache;
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
            charger.LastPingAt = DateTime.UtcNow;
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

            }

            return Ok(new { ok = true });
        }
        public sealed record DockHandshakeRequest(string SecretKey);

        [HttpPost("{dockId:int}/handshake")]
        public async Task<IActionResult> Handshake([FromRoute] int dockId,
                                           [FromBody] DockHandshakeRequest body,
                                           CancellationToken ct)
        {
            var charger = await _db.Chargers.FirstOrDefaultAsync(c => c.Id == dockId, ct);
            if (charger is null) return NotFound(new { ok = false, error = "Dock not found." });

            if (!VerifySecret(charger.DockSecretHash, body.SecretKey))
                return Unauthorized(new { ok = false, error = "Invalid secret." });

            // Idempotent session
            var session = await _db.ChargingSessions
                .Where(s => s.ChargerId == charger.Id && s.EndedAt == null)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync(ct);

            if (session is null)
            {
                session = new ChargingSession
                {
                    ChargerId = charger.Id,
                    Status = "PENDING",
                    SocStart = 0,
                    DurationMinutes = 0
                };
                _db.ChargingSessions.Add(session);
                await _db.SaveChangesAsync(ct);
            }

            if (string.IsNullOrWhiteSpace(session.AblyChannel))
                session.AblyChannel = $"ge:session:{session.Id}";
            if (string.IsNullOrWhiteSpace(session.JoinCode))
                session.JoinCode = await GenerateUniqueJoinCodeAsync(_db, 6, ct);
            await _db.SaveChangesAsync(ct);

            // Ably token + Dock JWT
            var channel = session.AblyChannel!;
            var capability = $@"{{""{channel}"":[""publish"",""subscribe""]}}";
            var ttl = TimeSpan.FromHours(1);
            var ablyToken = await _ably.CreateTokenAsync(channel, $"dock-{dockId}", capability, ttl, ct);
            var dockJwt = IssueDockSessionJwt(dockId, session.Id, ttl);

            // store dock ably token in BE cache
            var expiresAt = DateTime.UtcNow.Add(ttl);
            await _ablyTokenCache.SaveAsync(
                key: $"realtime:session:{session.Id}:dock",
                token: new CachedAblyToken
                {
                    ChannelId = channel,
                    TokenJson = JsonSerializer.Serialize(ablyToken, Camel),
                    ExpiresAtUtc = expiresAt
                },
                ttl: ttl,
                ct);

            // cập nhật dock
            charger.AblyChannel = charger.AblyChannel ?? $"ge:dock:{dockId}";
            charger.DockStatus = "CONNECTED";
            charger.LastConnectedAt = DateTime.UtcNow;
            charger.LastPingAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Ok(new
            {
                status = "success",
                channelId = session.AblyChannel,
                ok = true,
                data = new
                {
                    sessionId = session.Id,
                    channelId = session.AblyChannel,
                    joinCode = session.JoinCode,
                    ablyToken,
                    dockJwt,
                    expiresAt = DateTime.UtcNow.Add(ttl),
                    charger = new
                    {
                        id = charger.Id,
                        code = charger.Code,
                        stationId = charger.StationId,
                        connectorTypeId = charger.ConnectorTypeId,
                        powerKw = charger.PowerKw,
                        status = charger.Status,
                        dockStatus = charger.DockStatus,
                        ablyChannel = charger.AblyChannel,
                        lastConnectedAt = charger.LastConnectedAt,
                        lastPingAt = charger.LastPingAt,
                        pricePerKwh = charger.PricePerKwh
                    }
                }
            });
        }


        private static async Task<string> GenerateUniqueJoinCodeAsync(AppDbContext db, int length, CancellationToken ct)
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
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

        [AllowAnonymous]
        [HttpPost("join")]
        public async Task<IActionResult> JoinByCode([FromBody] JoinByCodeRequest body,
                                                    CancellationToken ct)
        {
            var s = await _db.ChargingSessions
                .FirstOrDefaultAsync(x => x.JoinCode == body.Code && x.EndedAt == null, ct);
            if (s is null) return NotFound(new { ok = false, error = "Invalid or expired code." });

            var channel = s.AblyChannel!;
            var canPublish = body.Role == "dashboard";
            var cap = canPublish
                ? $@"{{""{channel}"":[""subscribe"",""publish""]}}"
                : $@"{{""{channel}"":[""subscribe""]}}";
            var ttl = TimeSpan.FromHours(1);

            var clientId = $"{body.Role}-{Guid.NewGuid():N}";
            var token = await _ably.CreateTokenAsync(channel, clientId, cap, ttl, ct);

            return Ok(new
            {
                ok = true,
                data = new { sessionId = s.Id, channelId = channel, token, expiresAt = DateTime.UtcNow.Add(ttl) }
            });
        }
        private string IssueDockSessionJwt(int dockId, int sessionId, TimeSpan ttl)
        {
            var issuer = _cfg["DockAuth:Issuer"];
            var audience = _cfg["DockAuth:Audience"];
            var key = _cfg["DockAuth:SigningKey"]!;
            Console.WriteLine($"[DockJwt-Issue] issuer={issuer}, audience={audience}, keyLen={key.Length}");

            var secKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)) { KeyId = "dock-v1" };
            var creds = new SigningCredentials(secKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("role", "Dock"),
                new Claim("dockId", dockId.ToString()),
                new Claim("sessionId", sessionId.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, $"dock:{dockId}")
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.Add(ttl),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public sealed record PingRequest(int DockId, string SecretKey);
        [HttpPost("ping")]
        public async Task<IActionResult> Ping([FromBody] PingRequest body,
                                      CancellationToken ct)
        {
            var charger = await _db.Chargers.FirstOrDefaultAsync(c => c.Id == body.DockId, ct);
            if (charger is null)
                return NotFound(new { ok = false, error = "Charger not found." });

            if (!VerifySecret(charger.DockSecretHash, body.SecretKey))
                return Unauthorized(new { ok = false, error = "Invalid secret." });

            // update heartbeat
            var now = DateTime.UtcNow;
            charger.LastPingAt = now;

            // Khi có ping thì coi như online/connected
            if (!string.Equals(charger.Status, "ONLINE", StringComparison.OrdinalIgnoreCase))
                charger.Status = "ONLINE";
            charger.DockStatus = "CONNECTED";

            await _db.SaveChangesAsync(ct);

            return Ok(new
            {
                ok = true,
                serverTime = now
            });
        }

    }
}
