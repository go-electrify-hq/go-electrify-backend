namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IInsightsRepository
    {
        Task<IReadOnlyList<(DateTime Bucket, decimal Amount)>> GetRevenueAsync(DateTime from, DateTime to, int? stationId, string granularity, CancellationToken ct);
        Task<IReadOnlyList<(DateTime Bucket, int Count)>> GetUsageSeriesAsync(DateTime from, DateTime to, int? stationId, string granularity, CancellationToken ct);
        Task<(int PeakHour, int PeakCount, int Total)> GetUsagePeaksAsync(DateTime from, DateTime to, int? stationId, CancellationToken ct);
    }
}
