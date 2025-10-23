using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Dto.Notification;
using GoElectrify.BLL.Dtos.Notification;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _db;
        public NotificationRepository(AppDbContext db) => _db = db;

        public async Task<IReadOnlyList<NotificationDto>> GetDashboardBaseAsync(
           NotificationQueryDto query, int userId, string role, CancellationToken cancellationToken)
        {
            DateTime sinceUtc;
            if (query.Since == null) sinceUtc = DateTime.UtcNow.AddDays(-7);
            else sinceUtc = query.Since.Value;

            int limit = query.Limit;
            if (limit < 1) limit = 20;
            if (limit > 100) limit = 100;

            // Build typeSet
            HashSet<string> typeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (query.Types != null)
            {
                foreach (string? t in query.Types)
                {
                    if (string.IsNullOrWhiteSpace(t)) continue;
                    typeSet.Add(t.Trim());
                }
            }

            // Chuẩn hóa minSeverity (LOW|MEDIUM|HIGH|CRITICAL)
            string minSeverity = "LOW";
            if (!string.IsNullOrWhiteSpace(query.MinSeverity))
            {
                string s = query.MinSeverity.Trim().ToUpperInvariant();
                if (s == "CRITICAL" || s == "HIGH" || s == "MEDIUM" || s == "LOW") minSeverity = s;
            }

            bool isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
            bool isStaff = string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase);

            // 2) Nếu Staff → lấy danh sách Station được gán
            List<int> staffStations = new List<int>();
            if (isStaff)
            {
                var qStaff = _db.StationStaff.AsNoTracking()
                    .Where(x => x.UserId == userId && x.RevokedAt == null)
                    .Select(x => x.StationId);

                staffStations = await qStaff.ToListAsync(cancellationToken);
            }

            // 3) Lấy từng nhóm rồi gộp
            List<NotificationDto> bucket = new List<NotificationDto>(limit * 3);

            // Booking created
            if (typeSet.Count == 0 || typeSet.Contains("booking_created"))
            {
                var q = _db.Bookings.AsNoTracking()
                    .Where(b => b.CreatedAt >= sinceUtc);
                if (!isAdmin) q = q.Where(b => staffStations.Contains(b.StationId));

                var items = await q
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(limit)
                    .Select(b => new NotificationDto
                    {
                        Id = "booking:" + b.Id,
                        Title = "New Booking",
                        Message = "A new booking was created at Station " + b.StationId + ".",
                        Type = "booking_created",
                        Severity = "LOW",
                        CreatedAt = b.CreatedAt
                    })
                    .ToListAsync(cancellationToken);

                bucket.AddRange(items);
            }

            // Charging completed
            if (typeSet.Count == 0 || typeSet.Contains("charging_completed"))
            {
                var q = _db.ChargingSessions.AsNoTracking()
                    .Include(cs => cs.Charger)
                    .Where(cs => cs.Status == "COMPLETED" && cs.UpdatedAt >= sinceUtc);
                if (!isAdmin) q = q.Where(cs => staffStations.Contains(cs.Charger.StationId));

                var items = await q
                    .OrderByDescending(cs => cs.UpdatedAt)
                    .Take(limit)
                    .Select(cs => new NotificationDto
                    {
                        Id = "session:" + cs.Id,
                        Title = "Charging Completed",
                        Message = "Charging session at Station " + cs.Charger.StationId + " has completed.",
                        Type = "charging_completed",
                        Severity = "LOW",
                        CreatedAt = cs.CreatedAt
                    })
                    .ToListAsync(cancellationToken);

                bucket.AddRange(items);
            }

            // Incident reported
            if (typeSet.Count == 0 || typeSet.Contains("incident_reported"))
            {
                var q = _db.Incidents.AsNoTracking()
                    .Where(i => i.ReportedAt >= sinceUtc);
                if (!isAdmin) q = q.Where(i => staffStations.Contains(i.StationId));

                // Lấy thô về rồi map severity (viết ngay tại đây cho gọn)
                var raw = await q
                    .OrderByDescending(i => i.ReportedAt)
                    .Take(limit)
                    .Select(i => new
                    {
                        i.Id,
                        i.Title,
                        i.Priority,
                        i.StationId,
                        i.ReportedAt
                    })
                    .ToListAsync(cancellationToken);

                foreach (var i in raw)
                {
                    string sev = "LOW";
                    if (!string.IsNullOrWhiteSpace(i.Priority))
                    {
                        string up = i.Priority.Trim().ToUpperInvariant();
                        if (up == "CRITICAL") sev = "CRITICAL";
                        else if (up == "HIGH") sev = "HIGH";
                        else if (up == "MEDIUM") sev = "MEDIUM";
                        else sev = "LOW";
                    }

                    bucket.Add(new NotificationDto
                    {
                        Id = "incident:" + i.Id,
                        Title = "Incident Reported",
                        Message = i.Title,
                        Type = "incident_reported",
                        Severity = sev,
                        CreatedAt = i.ReportedAt
                    });
                }
            }

            // New user (Admin-only)
            if (isAdmin && (typeSet.Count == 0 || typeSet.Contains("user_registered")))
            {
                var items = await _db.Users.AsNoTracking()
                    .Where(u => u.CreatedAt >= sinceUtc)
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(limit)
                    .Select(u => new NotificationDto
                    {
                        Id = "user:" + u.Id,
                        Title = "New User",
                        Message = "A new user has registered.",
                        Type = "user_registered",
                        Severity = "LOW",
                        CreatedAt = u.CreatedAt
                    })
                    .ToListAsync(cancellationToken);

                bucket.AddRange(items);
            }

            // 4) Lọc theo ngưỡng severity
            List<NotificationDto> filtered = new List<NotificationDto>(bucket.Count);
            foreach (var n in bucket)
            {
                int rank = 1;
                if (n.Severity == "MEDIUM") rank = 2;
                else if (n.Severity == "HIGH") rank = 3;
                else if (n.Severity == "CRITICAL") rank = 4;

                int minRank = 1;
                if (minSeverity == "MEDIUM") minRank = 2;
                else if (minSeverity == "HIGH") minRank = 3;
                else if (minSeverity == "CRITICAL") minRank = 4;

                if (rank >= minRank) filtered.Add(n);
            }

            // 5) Sort + cắt limit
            List<NotificationDto> finalList = filtered
                .OrderByDescending(n => n.CreatedAt)
                .ThenBy(n => n.Id)
                .Take(limit)
                .ToList();

            return finalList;
        }
    }
}
