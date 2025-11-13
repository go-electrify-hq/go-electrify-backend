using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Insights;

namespace GoElectrify.BLL.Services
{
    public sealed class InsightsService : IInsightsService
    {
        private readonly IInsightsRepository _repo;
        public InsightsService(IInsightsRepository repo) => _repo = repo;

        // =============== REVENUE (day / hour) ===============
        public async Task<RevenueSeriesDto> GetRevenueAsync(
            DateTime from, DateTime to, int? stationId, string granularity, CancellationToken ct = default)
        {
            if (to <= from) throw new ArgumentException("`to` must be greater than `from`.");

            var fromUtc = NormalizeUtc(from);
            var toUtc = NormalizeUtc(to);

            granularity = (granularity ?? "day").Trim().ToLowerInvariant();

            var raw = await _repo.GetRevenueAsync(fromUtc, toUtc, stationId, granularity, ct);
            var map = raw.ToDictionary(x => x.Bucket, x => x.Amount);

            var series = new List<RevenuePointDto>();

            if (granularity == "hour")
            {
                // ví dụ: fromUtc = 2025-11-13T00:00Z, toUtc = 2025-11-14T00:00Z
                for (var t = fromUtc; t < toUtc; t = t.AddHours(1))
                {
                    map.TryGetValue(t, out var amount);
                    series.Add(new RevenuePointDto
                    {
                        Bucket = t,
                        Amount = amount
                    });
                }
            }
            else // "day"
            {
                // fill đủ từng ngày trong khoảng
                for (var d = fromUtc.Date; d < toUtc.Date; d = d.AddDays(1))
                {
                    map.TryGetValue(d, out var amount);
                    series.Add(new RevenuePointDto
                    {
                        Bucket = d,
                        Amount = amount
                    });
                }
            }

            return new RevenueSeriesDto
            {
                Series = series,
                Total = series.Sum(x => x.Amount)
            };
        }

        // =============== USAGE (hour / day) ===============
        public async Task<UsageSeriesDto> GetUsageAsync(
            DateTime from, DateTime to, int? stationId, string granularity, CancellationToken ct)
        {
            if (to <= from) throw new ArgumentException("`to` must be greater than `from`.");

            var fromUtc = NormalizeUtc(from);
            var toUtc = NormalizeUtc(to);

            granularity = (granularity ?? "hour").Trim().ToLowerInvariant();

            var rawSeries = await _repo.GetUsageSeriesAsync(fromUtc, toUtc, stationId, granularity, ct);
            var peak = await _repo.GetUsagePeaksAsync(fromUtc, toUtc, stationId, ct);

            var map = rawSeries.ToDictionary(x => x.Bucket, x => x.Count);
            var filled = new List<UsagePointDto>();

            if (granularity == "hour")
            {
                // luôn fill đủ từng giờ
                for (var t = fromUtc; t < toUtc; t = t.AddHours(1))
                {
                    map.TryGetValue(t, out var count);
                    filled.Add(new UsagePointDto
                    {
                        Bucket = t,
                        Count = count
                    });
                }
            }
            else // "day"
            {
                for (var d = fromUtc.Date; d < toUtc.Date; d = d.AddDays(1))
                {
                    map.TryGetValue(d, out var count);
                    filled.Add(new UsagePointDto
                    {
                        Bucket = d,
                        Count = count
                    });
                }
            }

            return new UsageSeriesDto
            {
                Series = filled,
                PeakHour = peak.PeakHour,
                PeakHourCount = peak.PeakCount,
                TotalSessions = peak.Total
            };
        }

        // =============== helper ===============
        private static DateTime NormalizeUtc(DateTime dt)
        {
            return dt.Kind switch
            {
                DateTimeKind.Utc => dt,
                DateTimeKind.Local => dt.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
                _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc) // fallback
            };
        }
    }
}
