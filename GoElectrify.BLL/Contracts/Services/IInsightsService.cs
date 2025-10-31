using GoElectrify.BLL.Dto.Insights;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IInsightsService
    {
        Task<RevenueSeriesDto> GetRevenueAsync(DateTime from, DateTime to, int? stationId, string granularity, CancellationToken ct);
        Task<UsageSeriesDto> GetUsageAsync(DateTime from, DateTime to, int? stationId, string granularity, CancellationToken ct);
    }
}
