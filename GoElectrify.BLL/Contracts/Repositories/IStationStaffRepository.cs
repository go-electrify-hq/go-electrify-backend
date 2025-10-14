using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IStationStaffRepository
    {
        Task<StationStaff?> GetAsync(int stationId, int userId, CancellationToken ct);
        Task<List<StationStaff>> ListByStationAsync(int stationId, CancellationToken ct);
        Task AddAsync(StationStaff entity, CancellationToken ct);
        void Update(StationStaff entity);
        void Remove(StationStaff entity);
        Task SaveAsync(CancellationToken ct);
        Task<StationStaff?> GetActiveByUserIdAsync(int userId, CancellationToken ct);
    }
}
