using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IBookingRepository
    {
        Task<Booking?> GetByIdAsync(int id, CancellationToken ct);
        Task AddAsync(Booking entity, CancellationToken ct);
        Task UpdateAsync(Booking entity, CancellationToken ct);
        Task<bool> DeleteAsync(int id, CancellationToken ct);

        Task<IReadOnlyList<Booking>> GetMyAsync(
            int userId, string? status, DateTime? from, DateTime? to,
            int page, int pageSize, CancellationToken ct);

        // Capacity & overlap
        Task<int> CountActiveBookingsAsync(
            int stationId, int connectorTypeId, DateTime windowStartUtc, DateTime windowEndUtc, CancellationToken ct);

        Task<int> CountActiveChargersAsync(
            int stationId, int connectorTypeId, CancellationToken ct);

        Task<bool> VehicleSupportsConnectorAsync(int vehicleModelId, int connectorTypeId, CancellationToken ct);

        Task<bool> StationExistsAsync(int stationId, CancellationToken ct);
    }
}
