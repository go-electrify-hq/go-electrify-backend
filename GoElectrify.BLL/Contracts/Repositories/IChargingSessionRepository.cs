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

    }
}
