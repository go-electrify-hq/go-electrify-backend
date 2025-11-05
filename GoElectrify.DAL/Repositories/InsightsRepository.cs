using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Dto.Insights;
using GoElectrify.DAL.Persistence;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public sealed class InsightsRepository : IInsightsRepository
    {
        private readonly AppDbContext _db;
        public InsightsRepository(AppDbContext db) => _db = db;

        // ===================== DOANH THU =====================
        public async Task<IReadOnlyList<(DateTime Bucket, decimal Amount)>> GetRevenueAsync(
    DateTime fromUtc, DateTime toUtc, int? stationId, string granularity, CancellationToken ct)
        {
            if (granularity == "hour")
            {
                var q = from s in _db.ChargingSessions.AsNoTracking()
                        where s.Status == "COMPLETED"
                           && s.StartedAt >= fromUtc
                           && s.StartedAt < toUtc
                           && (stationId == null || s.Charger!.StationId == stationId)
                        group s by new
                        {
                            s.StartedAt.Year,
                            s.StartedAt.Month,
                            s.StartedAt.Day,
                            s.StartedAt.Hour
                        } into g
                        orderby g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour
                        select new
                        {
                            Bucket = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, 0, 0, DateTimeKind.Utc),
                            Amount = g.Sum(x => x.Cost ?? x.EnergyKwh * (x.Charger!.PricePerKwh ?? 0m))
                        };

                var list = await q.ToListAsync(ct);
                return list.Select(x => (x.Bucket, x.Amount)).ToList();
            }
            else // day (mặc định)
            {
                var q = from s in _db.ChargingSessions.AsNoTracking()
                        where s.Status == "COMPLETED"
                           && s.StartedAt >= fromUtc
                           && s.StartedAt < toUtc
                           && (stationId == null || s.Charger!.StationId == stationId)
                        group s by new
                        {
                            s.StartedAt.Year,
                            s.StartedAt.Month,
                            s.StartedAt.Day
                        } into g
                        orderby g.Key.Year, g.Key.Month, g.Key.Day
                        select new
                        {
                            Bucket = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, 0, 0, 0, DateTimeKind.Utc),
                            Amount = g.Sum(x => x.Cost ?? x.EnergyKwh * (x.Charger!.PricePerKwh ?? 0m))
                        };

                var list = await q.ToListAsync(ct);
                return list.Select(x => (x.Bucket, x.Amount)).ToList();
            }
        }


        // ===================== USAGE SERIES =====================
        public async Task<IReadOnlyList<(DateTime Bucket, int Count)>> GetUsageSeriesAsync(
    DateTime from, DateTime to, int? stationId, string granularity, CancellationToken ct)
        {
            granularity = (granularity ?? "hour").Trim().ToLowerInvariant();

            var q = _db.ChargingSessions
                .AsNoTracking()
                .Where(s => s.Status != "CANCELED" && s.StartedAt >= from && s.StartedAt < to);

            if (stationId is int sid)
                q = q.Where(s => s.Charger!.StationId == sid);

            if (granularity == "hour")
            {
                var groupedRaw = await q
                    .GroupBy(s => new { s.StartedAt.Year, s.StartedAt.Month, s.StartedAt.Day, s.StartedAt.Hour })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        g.Key.Day,
                        g.Key.Hour,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day).ThenBy(x => x.Hour)
                    .ToListAsync(ct);

                return groupedRaw
                    .Select(x => (new DateTime(x.Year, x.Month, x.Day, x.Hour, 0, 0, DateTimeKind.Utc), x.Count))
                    .ToList();
            }
            else // "day"
            {
                var groupedRaw = await q
                    .GroupBy(s => new { s.StartedAt.Year, s.StartedAt.Month, s.StartedAt.Day })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        g.Key.Day,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day)
                    .ToListAsync(ct);

                return groupedRaw
                    .Select(x => (new DateTime(x.Year, x.Month, x.Day, 0, 0, 0, DateTimeKind.Utc), x.Count))
                    .ToList();
            }
        }


        // ===================== PEAK HOUR =====================
        public async Task<(int PeakHour, int PeakCount, int Total)> GetUsagePeaksAsync(
     DateTime from, DateTime to, int? stationId, CancellationToken ct)
        {
            var q = _db.ChargingSessions
                .AsNoTracking()
                .Where(s => s.Status != "CANCELED" && s.StartedAt >= from && s.StartedAt < to);

            if (stationId is int sid)
                q = q.Where(s => s.Charger!.StationId == sid);

            var byHour = await q
                .GroupBy(s => s.StartedAt.Hour)
                .Select(g => new { Hour = g.Key, C = g.Count() })
                .OrderByDescending(x => x.C).ThenBy(x => x.Hour)
                .FirstOrDefaultAsync(ct);

            var total = await q.CountAsync(ct);
            return (byHour?.Hour ?? 0, byHour?.C ?? 0, total);
        }
   }
}
