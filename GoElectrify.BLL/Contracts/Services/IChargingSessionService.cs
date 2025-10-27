using GoElectrify.BLL.Dto.ChargingSession;
using GoElectrify.BLL.Dtos.ChargingSession;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IChargingSessionService
    {
        Task<ChargingSessionDto> StartForDriverAsync(int userId, StartSessionDto dto, CancellationToken ct);
        Task<StopResult> StopAsync(int sessionId, string? reason, int? finalSoc, decimal? energyKwh, CancellationToken ct);
        Task<ChargingSessionDto> StopAsync(int userId, int sessionId, string reason, CancellationToken ct);
        Task<IReadOnlyList<ChargingSessionDto>> GetByStationAsync(
        int stationId,
        StationSessionQueryDto q,
        CancellationToken ct);
    }
}
