using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.DAL.Repositories
{
    public class IncidentRepository : IIncidentRepository
    {
        private readonly AppDbContext _db;
        public IncidentRepository(AppDbContext db) => _db = db;

        public Task<Incident?> GetByIdAsync(int incidentId, CancellationToken ct)
        {
            return _db.Incidents.AsNoTracking().FirstOrDefaultAsync(x => x.Id == incidentId, ct);
        }

        public async Task<List<Incident>> ListByStationAsync(
            int stationId,
            string? status,
            string? severity,
            DateTime? fromReportedAt,
            DateTime? toReportedAt,
            CancellationToken ct)
        {
            var q = _db.Incidents.AsNoTracking().Where(x => x.StationId == stationId);

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(x => x.Status == status);

            if (!string.IsNullOrWhiteSpace(severity))
                q = q.Where(x => x.Priority == severity);

            if (fromReportedAt.HasValue)
                q = q.Where(x => x.ReportedAt >= fromReportedAt.Value);

            if (toReportedAt.HasValue)
                q = q.Where(x => x.ReportedAt <= toReportedAt.Value);

            return await q.OrderByDescending(x => x.ReportedAt).ThenByDescending(x => x.Id).ToListAsync(ct);
        }

        public async Task AddAsync(Incident entity, CancellationToken ct)
        {
            await _db.Incidents.AddAsync(entity, ct);
            await _db.SaveChangesAsync(ct);
        }

        public void Update(Incident entity)
        {
            _db.Incidents.Update(entity);
        }

        public Task SaveAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}
