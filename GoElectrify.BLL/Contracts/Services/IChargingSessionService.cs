using GoElectrify.BLL.Dto.ChargingSession;
using GoElectrify.BLL.Dtos.ChargingSession;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IChargingSessionService
    {
        Task<ChargingSessionDto> StopAsync(int userId, int sessionId, string reason, CancellationToken ct);
        Task<IReadOnlyList<ChargingSessionDto>> GetByStationAsync(
        int stationId,
        StationSessionQueryDto q,
        CancellationToken ct);
        Task<(bool Ok, string? Error, SessionLogWindow Window, IReadOnlyList<SessionLogItemDto> Items)>
            GetLogsAsync(int sessionId, int last, CancellationToken ct);

        Task<(bool Ok, string? Error, StartSessionResult? Data, object? EventPayload)>
            StartAsync(int sessionId, int dockIdFromToken, BLL.Dtos.Dock.StartSessionRequest req, CancellationToken ct);

        Task<(bool Ok, string? Error, BindBookingResult? Data, object? EventPayload)>
            BindBookingAsync(int userId, int sessionId, BindBookingRequest body, CancellationToken ct);

        Task<(ChargingSessionLightDto? Active, ChargingSessionLightDto? Unpaid)>
            GetMyCurrentAsync(int userId, bool includeUnpaid, CancellationToken ct);

        Task<PagedResult<ChargingSessionHistoryItemDto>>
            GetMyHistoryAsync(int userId, HistoryQueryDto q, CancellationToken ct);
    }
}
