using GoElectrify.BLL.Dto.Station;
using GoElectrify.BLL.Entities;
namespace GoElectrify.BLL.Contracts.Services;
public interface IStationService
{
    Task<IEnumerable<Station>> GetAllStationsAsync();
    Task<Station?> GetStationByIdAsync(int id);
    Task<Station> CreateStationAsync(StationCreateDto request);
    Task<Station?> UpdateStationAsync(int id, StationUpdateDto request);
    Task<bool> DeleteStationAsync(int id);

    Task<IReadOnlyList<StationNearbyDto>> GetNearbyAsync(
       double lat, double lng, double radiusKm = 10, int limit = 20, CancellationToken ct = default);
}

