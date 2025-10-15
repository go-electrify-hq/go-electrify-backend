using GoElectrify.BLL.Dto.StationStaff;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IStationStaffService
    {
        Task<List<StationStaffDto>> ListAsync(int stationId, CancellationToken ct);
        Task<StationStaffDto> AssignAsync(int stationId, AssignStaffRequestDto req, CancellationToken ct);
        Task<RevokeStaffResultDto> DeleteAsync(int stationId, int userId, string reason, CancellationToken ct);
    }
}
