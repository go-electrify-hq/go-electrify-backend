using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Contracts.Services;
using GoElectrify.BLL.Dto.Incidents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Services
{
    public class AdminIncidentService : IAdminIncidentService
    {
        private readonly IAdminIncidentRepository _repo;

        public AdminIncidentService(IAdminIncidentRepository repo)
        {
            _repo = repo;
        }

        public Task<(List<AdminIncidentListItemDto> Items, int? Total)> ListAsync(
            AdminIncidentListQueryDto query, bool includeTotal, CancellationToken ct)
        {
            return _repo.SearchAsync(query, includeTotal, ct);
        }

        public async Task<AdminIncidentListItemDto> GetAsync(int incidentId, CancellationToken ct)
        {
            var dto = await _repo.GetProjectedByIdAsync(incidentId, ct);
            if (dto == null) throw new KeyNotFoundException("Incident not found.");
            return dto;
        }
    }
}
