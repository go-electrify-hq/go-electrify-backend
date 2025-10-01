using GoElectrify.BLL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IIncidentRepository
    {
        Task<Incident?> GetByIdAsync(int incidentId, CancellationToken ct);
        Task<List<Incident>> ListByStationAsync(
            int stationId,
            string? status,
            string? severity,
            DateTime? fromReportedAt,
            DateTime? toReportedAt,
            CancellationToken ct);

        Task AddAsync(Incident entity, CancellationToken ct);
        void Update(Incident entity);
        Task SaveAsync(CancellationToken ct);
    }
}
