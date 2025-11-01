using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IStationStaffRepository
    {
        Task<StationStaff?> GetAsync(int stationId, int userId, CancellationToken ct);
        Task<StationStaff?> GetActiveByUserAsync(int userId, CancellationToken ct); // NEW
        Task<List<StationStaff>> ListByStationAsync(int stationId, bool includeRevoked, CancellationToken ct);
        Task AddAsync(StationStaff entity, CancellationToken ct);
        void Update(StationStaff entity);
        Task SaveAsync(CancellationToken ct);
    }
}
