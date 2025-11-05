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
                ReporterName = x.ReportedBy.User.FullName ?? x.ReportedBy.User.Email,
                Title = x.Title,
                Description = x.Description,
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
            // 1) Lấy entity (tracking) để cập nhật
            var inc = await _db.Set<Incident>()
                .FirstOrDefaultAsync(x => x.Id == incidentId, ct);

            if (inc == null)
                throw new KeyNotFoundException("Incident not found.");

            // 2) Chuẩn hoá status mục tiêu
            var target = (newStatus ?? string.Empty).Trim().ToUpperInvariant();

            // 3) Kiểm tra flow HỢP LỆ:
            //    - RESOLVED -> CLOSED
            //    - CLOSED   -> IN_PROGRESS
            if (target == "CLOSED")
            {
                if (!string.Equals(inc.Status, "RESOLVED", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Only RESOLVED incidents can be set to CLOSED by admin.");

                inc.Status = "CLOSED";

                // Đảm bảo có mốc thời gian hoàn tất nếu staff chưa set
                inc.ResolvedAt ??= DateTime.UtcNow;

                // (Optional) nếu entity có các field audit thì set ở đây:
                // inc.ClosedAt = DateTime.UtcNow;
                // inc.ClosedByUserId = adminUserId;
                // inc.CloseNote = note;
            }
            else if (target == "IN_PROGRESS")
            {
                if (!string.Equals(inc.Status, "CLOSED", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Only CLOSED incidents can be set back to IN_PROGRESS (reopen).");

                inc.Status = "IN_PROGRESS";

                // (Optional) audit reopen:
                // inc.ReopenedAt = DateTime.UtcNow;
                // inc.ReopenedByUserId = adminUserId;
                // inc.ReopenNote = note;
            }
            else
            {
                throw new ArgumentException("Target status must be 'CLOSED' or 'IN_PROGRESS'.");
            }

            // 4) Ghi DB
            _db.Update(inc);
            await _db.SaveChangesAsync(ct);

            // 5) Trả lại DTO mới nhất (dùng projection có StationName/ReporterUserId)
            var updated = await GetProjectedByIdAsync(incidentId, ct);
            // Về lý thuyết không null vì vừa cập nhật xong
            return updated ?? new AdminIncidentListItemDto
            {
                Id = inc.Id,
                StationId = inc.StationId,
                StationName = "", // fallback nếu không load được
                ChargerId = inc.ChargerId,
                ReporterName = inc.ReportedBy?.User?.FullName ?? inc.ReportedBy?.User?.Email,
                Title = inc.Title,
                Description = inc.Description,
                Severity = inc.Priority,
                Status = inc.Status,
                ReportedAt = inc.ReportedAt,
                ResolvedAt = inc.ResolvedAt
            };
        }
    }
}
