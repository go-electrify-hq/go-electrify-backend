using GoElectrify.BLL.Dto.Station;
using GoElectrify.BLL.Entities;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IStationRepository
    {
        Task<IEnumerable<Station>> GetAllAsync();
        Task<Station?> GetByIdAsync(int id);
        Task AddAsync(Station station);
        Task UpdateAsync(Station station);
        Task DeleteAsync(Station station);
        Task SaveChangesAsync();
        Task<IReadOnlyList<StationNearResult>> FindNearestAsync(
        double lat, double lng, double radiusKm, int limit, CancellationToken ct);
        Task<bool> ExistsAsync(int id, CancellationToken ct);
        Task<string?> GetNameByIdAsync(int stationId, CancellationToken ct);

    }
}