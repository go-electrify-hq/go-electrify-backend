using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Dto.Incidents;
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
    public class AdminIncidentRepository : IAdminIncidentRepository
    {
        private readonly AppDbContext _db;
        public AdminIncidentRepository(AppDbContext db) => _db = db;

        public async Task<(List<AdminIncidentListItemDto> Items, int? Total)> SearchAsync(
            AdminIncidentListQueryDto query, bool includeTotal, CancellationToken ct)
        {
            // 1) Base query: AsNoTracking vì đọc-only
            var q = _db.Set<Incident>().AsNoTracking().AsQueryable();

            // 2) Filter điều kiện (param nào có thì WHERE cái đó)
            if (query.StationId is int sid)
                q = q.Where(x => x.StationId == sid);

            if (!string.IsNullOrWhiteSpace(query.Status))
                q = q.Where(x => x.Status == query.Status);

            if (!string.IsNullOrWhiteSpace(query.Severity))
                q = q.Where(x => x.Priority == query.Severity); // entity dùng Priority cho mức độ

            if (query.FromReportedAt is DateTime from)
                q = q.Where(x => x.ReportedAt >= from);

            if (query.ToReportedAt is DateTime to)
                q = q.Where(x => x.ReportedAt <= to);

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var kw = query.Keyword.Trim();
                q = q.Where(x =>
                    EF.Functions.Like(x.Title, $"%{kw}%") ||
                    (x.Description != null && EF.Functions.Like(x.Description, $"%{kw}%"))
                );
            }

            // 3) Optional total
            int? total = null;
            if (includeTotal)
                total = await q.CountAsync(ct); // SELECT COUNT(*)

            // 4) Sort + paging
            q = q.OrderByDescending(x => x.ReportedAt)
                 .ThenByDescending(x => x.Id)
                 .Skip(query.Skip)
                 .Take(query.Take);

            // 5) Projection trực tiếp sang DTO (EF join đúng phần cần)
            var items = await q.Select(x => new AdminIncidentListItemDto
            {
                Id = x.Id,
                StationId = x.StationId,
                StationName = x.Station.Name,
                ChargerId = x.ChargerId,
                ReporterUserId = x.ReportedBy.UserId,
                Title = x.Title,
                Severity = x.Priority,  // map Priority -> Severity cho UI
                Status = x.Status,
                ReportedAt = x.ReportedAt,
                ResolvedAt = x.ResolvedAt
            }).ToListAsync(ct);

            return (items, total);
        }

        public async Task<AdminIncidentListItemDto?> GetProjectedByIdAsync(int incidentId, CancellationToken ct)
        {
            // Detail: NoTracking + projection DTO
            var dto = await _db.Incidents.AsNoTracking()
                .Where(x => x.Id == incidentId)
                .Select(x => new AdminIncidentListItemDto
                {
                    Id = x.Id,
                    StationId = x.StationId,
                    StationName = x.Station.Name,
                    ChargerId = x.ChargerId,
                    ReporterUserId = x.ReportedBy.UserId,
                    Title = x.Title,
                    Severity = x.Priority,
                    Status = x.Status,
                    ReportedAt = x.ReportedAt,
                    ResolvedAt = x.ResolvedAt
                })
                .FirstOrDefaultAsync(ct);

            return dto; // service sẽ quyết định ném 404 hay không
        }

    }
}
