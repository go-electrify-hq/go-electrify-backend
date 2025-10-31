using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Insights;

public sealed class InsightsService : IInsightsService
{
    private readonly IInsightsRepository _repo;
    public InsightsService(IInsightsRepository repo) => _repo = repo;

    public async Task<RevenueSeriesDto> GetRevenueAsync(DateTime from, DateTime to, int? stationId, string granularity, CancellationToken ct)
    {
        (from, to) = (NormalizeUtc(from), NormalizeUtc(to));

        var rows = await _repo.GetRevenueAsync(from, to, stationId, granularity, ct);
        var series = rows.Select(x => new RevenuePointDto { Bucket = x.Bucket, Amount = x.Amount })
                         .OrderBy(x => x.Bucket).ToList();
        return new RevenueSeriesDto { Series = series, Total = series.Sum(x => x.Amount) };
    }

    public async Task<UsageSeriesDto> GetUsageAsync(DateTime from, DateTime to, int? stationId, string granularity, CancellationToken ct)
    {
        (from, to) = (NormalizeUtc(from), NormalizeUtc(to));

        var rows = await _repo.GetUsageSeriesAsync(from, to, stationId, granularity, ct);
        var peak = await _repo.GetUsagePeaksAsync(from, to, stationId, ct);

        return new UsageSeriesDto
        {
            Series = rows.Select(x => new UsagePointDto { Bucket = x.Bucket, Count = x.Count })
                                 .OrderBy(x => x.Bucket).ToList(),
            PeakHour = peak.PeakHour,
            PeakHourCount = peak.PeakCount,
            TotalSessions = peak.Total
        };
    }

    private static DateTime NormalizeUtc(DateTime dt)
    {
        return dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dt, DateTimeKind.Utc) // giả định input là UTC nếu không chỉ định
        };
    }
}
