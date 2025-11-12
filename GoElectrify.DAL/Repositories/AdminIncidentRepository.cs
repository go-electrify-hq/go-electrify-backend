using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Dto.Incidents;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public class AdminIncidentRepository : IAdminIncidentRepository
    {
        private readonly AppDbContext _db;
        public AdminIncidentRepository(AppDbContext db) => _db = db;

        public async Task<(List<AdminIncidentListItemDto> Items, int? Total)> SearchAsync(
            AdminIncidentListQueryDto query, bool includeTotal, CancellationToken ct)
        {
            var q = _db.Set<Incident>().AsNoTracking().AsQueryable();

            var status = string.IsNullOrWhiteSpace(query.Status) ? null : query.Status.Trim().ToUpperInvariant();
            var sev = string.IsNullOrWhiteSpace(query.Severity) ? null : query.Severity.Trim().ToUpperInvariant();

            if (query.StationId is int sid) q = q.Where(x => x.StationId == sid);
            if (status != null) q = q.Where(x => x.Status == status);
            if (sev != null) q = q.Where(x => x.Priority == sev);

            if (query.FromReportedAt is DateTime from) q = q.Where(x => x.ReportedAt >= from);
            if (query.ToReportedAt is DateTime to)
            {
                var toNext = to.Date.AddDays(1);        // < ngày kế tiếp để bao trọn cuối ngày
                q = q.Where(x => x.ReportedAt < toNext);
            }

            if (!string.IsNullOrWhiteSpace(query.Keyword))
            {
                var kw = query.Keyword.Trim();
                q = q.Where(x =>
                    EF.Functions.Like(x.Title, $"%{kw}%") ||
                    (x.Description != null && EF.Functions.Like(x.Description, $"%{kw}%"))
                );
            }

            int? total = includeTotal ? await q.CountAsync(ct) : null;

            var items = await q.OrderByDescending(x => x.ReportedAt)
                .ThenByDescending(x => x.Id)
                .Skip(query.Skip).Take(query.Take)
                .Select(x => new AdminIncidentListItemDto
                {
                    Id = x.Id,
                    StationId = x.StationId,
                    StationName = x.Station.Name,
                    ChargerId = x.ChargerId,
                    ReporterName = x.ReportedBy != null && x.ReportedBy.User != null
                        ? (x.ReportedBy.User.FullName ?? x.ReportedBy.User.Email)
                        : null,
                    Title = x.Title,
                    Description = x.Description,
                    Severity = x.Priority,
                    Status = x.Status,
                    ReportedAt = x.ReportedAt,
                    ResolvedAt = x.ResolvedAt
                })
                .ToListAsync(ct);

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
                    ReporterName = x.ReportedBy.User.FullName ?? x.ReportedBy.User.Email,
                    Title = x.Title,
                    Description = x.Description,
                    Severity = x.Priority,
                    Status = x.Status,
                    ReportedAt = x.ReportedAt,
                    ResolvedAt = x.ResolvedAt
                })
                .FirstOrDefaultAsync(ct);

            return dto; // service sẽ quyết định ném 404 hay không
        }

        public async Task<AdminIncidentListItemDto> UpdateStatusAsync(int incidentId, string newStatus,int adminUserId, string? note, CancellationToken ct)
        {
            var inc = await _db.Incidents.FirstOrDefaultAsync(x => x.Id == incidentId, ct)
             ?? throw new KeyNotFoundException("Incident not found.");

            var target = (newStatus ?? "").Trim().ToUpperInvariant();

            if (target == "CLOSED")
            {
                if (!string.Equals(inc.Status, "RESOLVED", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Only RESOLVED -> CLOSED.");

                inc.Status = "CLOSED";
                inc.ResolvedAt ??= DateTime.UtcNow;  // đảm bảo có mốc hoàn tất
            }
            else if (target == "IN_PROGRESS")
            {
                if (!string.Equals(inc.Status, "CLOSED", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Only CLOSED -> IN_PROGRESS (reopen).");

                if (string.IsNullOrWhiteSpace(note))
                    throw new ArgumentException("Note is required to reopen.");

                inc.Status = "IN_PROGRESS";
                inc.ResolvedAt = null;               // reopen thì bỏ ResolvedAt
            }
            else
            {
                throw new ArgumentException("Target status must be CLOSED or IN_PROGRESS.");
            }

            await _db.SaveChangesAsync(ct);

            // trả projection gọn
            return await GetProjectedByIdAsync(incidentId, ct)
                   ?? new AdminIncidentListItemDto { Id = incidentId, Status = inc.Status, StationId = inc.StationId };
        }
    }
}
