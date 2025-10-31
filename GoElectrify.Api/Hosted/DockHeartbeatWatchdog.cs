using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GoElectrify.Api.Hosted
{
    public class DockHeartbeatWatchdog : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<DockHeartbeatWatchdog> _log;
        private readonly IConfiguration _cfg;

        public DockHeartbeatWatchdog(IServiceProvider sp, ILogger<DockHeartbeatWatchdog> log, IConfiguration cfg)
        { _sp = sp; _log = log; _cfg = cfg; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var scanEvery = TimeSpan.FromSeconds(_cfg.GetValue("DockHeartbeat:ScanIntervalSeconds", 10));
            var offlineAfterSec = _cfg.GetValue("DockHeartbeat:OfflineAfterSeconds", 90);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var now = DateTime.UtcNow;
                    var cutoff = now.AddSeconds(-offlineAfterSec);

                    var stale = await db.Chargers
                        .Where(c =>
                            !EF.Functions.ILike(c.Status!, "OFFLINE") &&
                            (c.LastPingAt == null || c.LastPingAt < cutoff))
                        .ToListAsync(stoppingToken);

                    if (stale.Count > 0)
                    {
                        foreach (var c in stale)
                        {
                            c.Status = "OFFLINE";
                            c.DockStatus = "DISCONNECTED";
                        }
                        await db.SaveChangesAsync(stoppingToken);
                        _log.LogInformation("DockHeartbeatWatchdog: set {Count} chargers OFFLINE", stale.Count);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "DockHeartbeatWatchdog error");
                }

                try { await Task.Delay(scanEvery, stoppingToken); } catch { /* ignore */ }
            }
        }
    }
}
