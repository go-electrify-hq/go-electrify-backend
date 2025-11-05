using GoElectrify.BLL.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IChargingSessionRepository
    {
        Task<Charger?> GetChargerAsync(int chargerId, CancellationToken ct);
        Task<int> CountActiveOnChargerAsync(int chargerId, CancellationToken ct);
        Task<bool> VehicleSupportsConnectorAsync(int vehicleModelId, int connectorTypeId, CancellationToken ct);
        Task<Booking?> FindBookingByCodeForUserAsync(string code, int userId, CancellationToken ct);
        Task AddSessionAsync(ChargingSession session, CancellationToken ct);
        Task<ChargingSession?> GetSessionAsync(int id, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
        Task<IDbContextTransaction> BeginSerializableTxAsync(CancellationToken ct);
        Task<Charger?> FindAvailableChargerAsync(int stationId, int connectorTypeId, CancellationToken ct);
        Task<VehicleModel?> GetVehicleModelAsync(int vehicleModelId, CancellationToken ct);
        Task<ConnectorType?> GetConnectorTypeAsync(int connectorTypeId, CancellationToken ct);
        Task<List<ChargingSession>> GetByStationAsync(
        int stationId,
        string? status,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct);

        Task<ChargingSession?> FindByIdAsync(int id, CancellationToken ct);
        Task<ChargingSession?> FindActiveByIdAsync(int id, CancellationToken ct);
        Task<bool> UserHasOtherUnpaidAsync(int userId, int exceptSessionId, CancellationToken ct);

        Task<ChargingSession?> GetActiveByUserAsync(int userId, CancellationToken ct);
        Task<ChargingSession?> GetClosestUnpaidByUserAsync(int userId, CancellationToken ct);

        Task<(int Total, List<ChargingSession> Items)> GetHistoryForUserAsync(
            int userId, DateTime? from, DateTime? to, HashSet<string> statuses, int page, int pageSize, CancellationToken ct);

    }
}
