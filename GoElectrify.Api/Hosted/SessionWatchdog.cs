using GoElectrify.Api.Realtime;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.Api.Hosted
{
    public class SessionWatchdog : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<SessionWatchdog> _log;

        private readonly TimeSpan _scanInterval = TimeSpan.FromSeconds(10);
        private const int HARD_MISS_SECONDS = 60;   // đã có tick mà mất >=60s -> IDLE_TIMEOUT
        private const int START_GRACE_SECONDS = 120;  // ân hạn sau khi start: chưa có tick đầu trong 120s -> NO_ACTIVITY

        public SessionWatchdog(IServiceProvider sp, ILogger<SessionWatchdog> log)
        {
            _sp = sp;
            _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var ably = scope.ServiceProvider.GetRequiredService<IAblyService>();
                    var svc = scope.ServiceProvider.GetRequiredService<IChargingSessionService>();

                    var now = DateTime.UtcNow;

                    // Các session đang chạy
                    var active = await db.ChargingSessions
                        .Where(s => s.EndedAt == null && s.Status == "RUNNING")
                        .Select(s => new { s.Id, s.ChargerId, s.StartedAt })
                        .ToListAsync(stoppingToken);

                    foreach (var s in active)
                    {
                        // Lấy tick MỚI NHẤT kể từ khi phiên bắt đầu (bỏ qua log cũ của phiên trước)
                        var lastAt = await db.ChargerLogs
                            .Where(l => l.ChargerId == s.ChargerId
                                     && l.SampleAt >= s.StartedAt) // 👈 quan trọng: chỉ xét log trong phạm vi phiên hiện tại
                            .OrderByDescending(l => l.SampleAt)
                            .Select(l => (DateTime?)l.SampleAt)
                            .FirstOrDefaultAsync(stoppingToken);

                        if (lastAt is null)
                        {
                            // Chưa có tick đầu tiên
                            var sinceStart = (now - s.StartedAt).TotalSeconds;
                            if (sinceStart < START_GRACE_SECONDS)
                                continue; // còn trong ân hạn -> bỏ qua

                            _log.LogWarning("Auto-stop session {Id} (no first tick after {Sec}s).", s.Id, sinceStart);

                            // Dừng phiên vì NO_ACTIVITY
                            var dto = await svc.StopAsync(userId: 0, sessionId: s.Id, reason: "NO_ACTIVITY", ct: stoppingToken);

                            // Nếu StopAsync đã publish Ably thì có thể bỏ khối publish dưới:
                            await ably.PublishAsync($"ge:dock:{dto.ChargerId}", "session.stopped", new
                            {
                                sessionId = dto.Id,
                                reason = "NO_ACTIVITY",
                                stoppedAt = DateTime.UtcNow
                            }, stoppingToken);

                            continue;
                        }

                        // Đã có tick -> kiểm tra idle
                        var miss = (now - lastAt.Value).TotalSeconds;
                        if (miss >= HARD_MISS_SECONDS)
                        {
                            _log.LogWarning("Auto-stop session {Id} due to idle timeout ({Sec}s).", s.Id, miss);

                            var dto = await svc.StopAsync(userId: 0, sessionId: s.Id, reason: "IDLE_TIMEOUT", ct: stoppingToken);

                            // Nếu StopAsync đã publish Ably thì có thể bỏ khối publish dưới:
                            await ably.PublishAsync($"ge:dock:{dto.ChargerId}", "session.stopped", new
                            {
                                sessionId = dto.Id,
                                reason = "IDLE_TIMEOUT",
                                stoppedAt = DateTime.UtcNow
                            }, stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "SessionWatchdog error");
                }

                try { await Task.Delay(_scanInterval, stoppingToken); } catch { /* ignore */ }
            }
        }
    }
}
