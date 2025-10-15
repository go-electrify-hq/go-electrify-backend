using GoElectrify.BLL.Dto.Incidents;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IIncidentService
    {
        Task<IncidentDto> CreateAsync(int stationId, int reporterUserId, IncidentCreateDto dto, CancellationToken ct);
        Task<List<IncidentDto>> ListAsync(int stationId, IncidentListQueryDto query, CancellationToken ct);
        Task<IncidentDto> GetAsync(int stationId, int incidentId, CancellationToken ct);
        Task<IncidentDto> UpdateStatusAsync(int stationId, int incidentId, int userId, IncidentUpdateStatusDto dto, CancellationToken ct);
    }
}
