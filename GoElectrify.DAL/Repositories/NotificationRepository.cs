using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Dto.Notification;
using GoElectrify.BLL.Dtos.Notification;
using GoElectrify.BLL.Entities;
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
            // ===== 1) Chuẩn hoá input =====
            DateTime sinceUtc = (query.Since.HasValue ? query.Since.Value.ToUniversalTime() : DateTime.UtcNow.AddDays(-7));

            int limit = query.Limit;
            if (limit < 1) limit = 20;
            if (limit > 100) limit = 100;

            // typeSet: nếu rỗng => lấy tất cả
            HashSet<string> typeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (query.Types != null)
            {
                foreach (var t in query.Types)
                {
                    if (!string.IsNullOrWhiteSpace(t)) typeSet.Add(t.Trim());
                }
            }

            string minSeverity = NormalizeSeverity(query.MinSeverity);
            int minRank = SeverityRank(minSeverity);

            bool isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
            bool isStaff = string.Equals(role, "Staff", StringComparison.OrdinalIgnoreCase);
            bool isDriver = string.Equals(role, "Driver", StringComparison.OrdinalIgnoreCase);

            // ===== 2) Bối cảnh theo role =====
            // Staff → các station được gán
            List<int> staffStations = new List<int>();
            if (isStaff && !isAdmin)
            {
                staffStations = await _db.StationStaff.AsNoTracking()
                    .Where(x => x.UserId == userId && x.RevokedAt == null)
                    .Select(x => x.StationId)
                    .ToListAsync(cancellationToken);
            }

            // Driver → các station liên quan (đặt chỗ 48h tới / phiên đang chạy)
            HashSet<int> driverRelatedStations = new HashSet<int>();
            if (isDriver)
            {
                DateTime within48h = DateTime.UtcNow.AddHours(48);

                var futureStations = await _db.Bookings.AsNoTracking()
                    .Where(b => b.UserId == userId
                             && b.ScheduledStart >= DateTime.UtcNow
                             && b.ScheduledStart <= within48h)
                    .Select(b => b.StationId)
                    .ToListAsync(cancellationToken);

                var runningStations = await _db.ChargingSessions.AsNoTracking()
                    .Include(s => s.Charger)
                    .Include(s => s.Booking)
                    .Where(s => s.Status == "RUNNING"
                             && s.Booking != null
                             && s.Booking.UserId == userId)
                    .Select(s => s.Charger.StationId)
                    .ToListAsync(cancellationToken);

                foreach (var sid in futureStations) driverRelatedStations.Add(sid);
                foreach (var sid in runningStations) driverRelatedStations.Add(sid);
            }

            // ===== 3) Thu thập thông báo =====
            List<NotificationDto> bucket = new List<NotificationDto>(limit * 6);

            // ---------- BOOKING ----------
            bucket.AddRange(await BuildBookingAsync(
                sinceUtc, limit, typeSet, isAdmin, isStaff, isDriver, staffStations, userId, cancellationToken));

            // ---------- CHARGING ----------
            bucket.AddRange(await BuildChargingAsync(
                sinceUtc, limit, typeSet, isAdmin, isStaff, isDriver, staffStations, userId, cancellationToken));

            // ---------- INCIDENT ----------
            bucket.AddRange(await BuildIncidentAsync(
                sinceUtc, limit, typeSet, isAdmin, isStaff, isDriver, staffStations, driverRelatedStations, cancellationToken));

            // ---------- PAYMENT (Topup + Transaction) ----------
            bucket.AddRange(await BuildPaymentAsync(
                sinceUtc, limit, typeSet, isAdmin, isStaff, isDriver, userId, cancellationToken));

            // ===== 4) Áp MinSeverity + Sort + Limit =====
            List<NotificationDto> filtered = new List<NotificationDto>(bucket.Count);
            foreach (var n in bucket)
            {
                if (SeverityRank(n.Severity) >= minRank) filtered.Add(n);
            }

            List<NotificationDto> finalList = filtered
                .OrderByDescending(n => n.CreatedAt)
                .ThenBy(n => n.Id)
                .Take(limit)
                .ToList();

            return finalList;
        }

        // =================== BOOKING ===================
        // Admin: tất cả; Staff: theo station assign; Driver: booking của chính mình
        private async Task<List<NotificationDto>> BuildBookingAsync(
            DateTime sinceUtc, int limit, HashSet<string> typeSet,
            bool isAdmin, bool isStaff, bool isDriver,
            List<int> staffStations, int userId, CancellationToken ct)
        {
            var result = new List<NotificationDto>(limit * 3);

            IQueryable<Booking> Scope(IQueryable<Booking> q)
            {
                if (isAdmin) return q;
                if (isStaff) return q.Where(b => staffStations.Contains(b.StationId));
                if (isDriver) return q.Where(b => b.UserId == userId);
                return q.Where(_ => false);
            }

            // booking_created (PENDING / CreatedAt)
            if (typeSet.Count == 0 || typeSet.Contains("booking_created"))
            {
                var q = Scope(_db.Bookings.AsNoTracking()
                        .Where(b => b.CreatedAt >= sinceUtc && b.Status == "PENDING"));

                var items = await q.OrderByDescending(b => b.CreatedAt)
                    .Take(limit)
                    .Select(b => new NotificationDto
                    {
                        Id = "booking:" + b.Id,
                        Title = "Đặt chỗ mới",
                        Message = $"Mã {b.Code} • Trạm #{b.StationId}",
                        Type = "booking_created",
                        Severity = "LOW",
                        CreatedAt = b.CreatedAt
                    })
                    .ToListAsync(ct);

                result.AddRange(items);
            }

            // booking_confirmed (CONFIRMED / UpdatedAt)
            if (typeSet.Count == 0 || typeSet.Contains("booking_confirmed"))
            {
                var q = Scope(_db.Bookings.AsNoTracking()
                        .Where(b => b.UpdatedAt >= sinceUtc && b.Status == "CONFIRMED"));

                var items = await q.OrderByDescending(b => b.UpdatedAt)
                    .Take(limit)
                    .Select(b => new NotificationDto
                    {
                        Id = "booking_cf:" + b.Id,
                        Title = "Đặt chỗ đã xác nhận",
                        Message = $"Mã {b.Code} • Trạm #{b.StationId}",
                        Type = "booking_confirmed",
                        Severity = "LOW",
                        CreatedAt = b.UpdatedAt
                    })
                    .ToListAsync(ct);

                result.AddRange(items);
            }

            // booking_canceled
            if (typeSet.Count == 0 || typeSet.Contains("booking_canceled"))
            {
                var q = Scope(_db.Bookings.AsNoTracking()
                        .Where(b => b.UpdatedAt >= sinceUtc && b.Status == "CANCELED"));

                var items = await q.OrderByDescending(b => b.UpdatedAt)
                    .Take(limit)
                    .Select(b => new NotificationDto
                    {
                        Id = "booking_cancel:" + b.Id,
                        Title = "Đặt chỗ đã hủy",
                        Message = $"Mã {b.Code} • Trạm #{b.StationId}",
                        Type = "booking_canceled",
                        Severity = "LOW",
                        CreatedAt = b.UpdatedAt
                    })
                    .ToListAsync(ct);

                result.AddRange(items);
            }

            // booking_expired
            if (typeSet.Count == 0 || typeSet.Contains("booking_expired"))
            {
                var q = Scope(_db.Bookings.AsNoTracking()
                        .Where(b => b.UpdatedAt >= sinceUtc && b.Status == "EXPIRED"));

                var items = await q.OrderByDescending(b => b.UpdatedAt)
                    .Take(limit)
                    .Select(b => new NotificationDto
                    {
                        Id = "booking_exp:" + b.Id,
                        Title = "Đặt chỗ hết hạn",
                        Message = $"Mã {b.Code} • Trạm #{b.StationId}",
                        Type = "booking_expired",
                        Severity = "LOW",
                        CreatedAt = b.UpdatedAt
                    })
                    .ToListAsync(ct);

                result.AddRange(items);
            }

            return result;
        }

        // =================== CHARGING ===================
        // Admin: tất cả; Staff: station assign; Driver: phiên thuộc booking của mình
        private async Task<List<NotificationDto>> BuildChargingAsync(
            DateTime sinceUtc, int limit, HashSet<string> typeSet,
            bool isAdmin, bool isStaff, bool isDriver,
            List<int> staffStations, int userId, CancellationToken ct)
        {
            var result = new List<NotificationDto>(limit);

            bool needStarted = typeSet.Count == 0 || typeSet.Contains("charging_started");
            bool needStopped = typeSet.Count == 0 || typeSet.Contains("charging_stopped");
            bool needCompleted = typeSet.Count == 0 || typeSet.Contains("charging_completed");
            bool needFailed = typeSet.Count == 0 || typeSet.Contains("charging_failed");

            if (!(needStarted || needStopped || needCompleted || needFailed))
                return result;

            var q = _db.ChargingSessions.AsNoTracking()
                .Include(s => s.Charger)
                .Include(s => s.Booking)
                .Where(s =>
                    ((s.UpdatedAt != default(DateTime)) ? s.UpdatedAt : s.StartedAt) >= sinceUtc);

            if (isStaff && !isAdmin)
                q = q.Where(s => staffStations.Contains(s.Charger.StationId));
            else if (isDriver)
                q = q.Where(s => s.Booking != null && s.Booking.UserId == userId);

            var raw = await q
                .OrderByDescending(s => (s.UpdatedAt != default(DateTime)) ? s.UpdatedAt : s.StartedAt)
                .Take(limit)
                .Select(s => new
                {
                    s.Id,
                    s.Status,
                    At = (s.UpdatedAt != default(DateTime)) ? s.UpdatedAt : s.StartedAt,
                    StationId = s.Charger.StationId
                })
                .ToListAsync(ct);

            foreach (var s in raw)
            {
                // Map status → type + title + severity
                string type;
                string title;
                string severity = "LOW";

                if (s.Status == "RUNNING")
                {
                    type = "charging_started";
                    title = "Bắt đầu sạc";
                    if (!needStarted) continue;
                }
                else if (s.Status == "STOPPED")
                {
                    type = "charging_stopped";
                    title = "Dừng sạc";
                    if (!needStopped) continue;
                }
                else if (s.Status == "COMPLETED")
                {
                    type = "charging_completed";
                    title = "Hoàn tất sạc";
                    if (!needCompleted) continue;
                }
                else if (s.Status == "FAILED")
                {
                    type = "charging_failed";
                    title = "Phiên sạc lỗi";
                    severity = "MEDIUM";
                    if (!needFailed) continue;
                }
                else
                {
                    // fallback
                    type = "charging_started";
                    title = "Phiên sạc";
                    if (!needStarted) continue;
                }

                result.Add(new NotificationDto
                {
                    Id = "session:" + s.Id,
                    Title = title,
                    Message = $"Trạm #{s.StationId}",
                    Type = type,
                    Severity = severity,
                    CreatedAt = s.At
                });
            }

            return result;
        }

        // =================== INCIDENT ===================
        // Admin: mọi trạm; Staff: station assign; Driver: trạm liên quan (đặt chỗ 48h tới / đang sạc)
        private async Task<List<NotificationDto>> BuildIncidentAsync(
            DateTime sinceUtc, int limit, HashSet<string> typeSet,
            bool isAdmin, bool isStaff, bool isDriver,
            List<int> staffStations, HashSet<int> driverRelatedStations, CancellationToken ct)
        {
            var result = new List<NotificationDto>(limit);
            if (typeSet.Count > 0 && !typeSet.Contains("incident_reported")) return result;

            var q = _db.Incidents.AsNoTracking()
                .Where(i => i.ReportedAt >= sinceUtc);

            if (isStaff && !isAdmin)
            {
                q = q.Where(i => staffStations.Contains(i.StationId));
            }
            else if (isDriver)
            {
                if (driverRelatedStations.Count == 0) return result;
                q = q.Where(i => driverRelatedStations.Contains(i.StationId));
            }

            var raw = await q
                .OrderByDescending(i => i.ReportedAt)
                .Take(limit)
                .Select(i => new { i.Id, i.Title, i.Priority, i.StationId, i.ReportedAt })
                .ToListAsync(ct);

            foreach (var i in raw)
            {
                string sev = MapPriorityToSeverity(i.Priority);
                result.Add(new NotificationDto
                {
                    Id = "incident:" + i.Id,
                    Title = "Sự cố tại trạm",
                    Message = $"{i.Title} • Trạm #{i.StationId}",
                    Type = "incident_reported",
                    Severity = sev,
                    CreatedAt = i.ReportedAt
                });
            }

            return result;
        }

        // =================== PAYMENT ===================
        // Admin: mọi ví; Driver: ví của mình; Staff: mặc định ẩn
        private async Task<List<NotificationDto>> BuildPaymentAsync(
            DateTime sinceUtc, int limit, HashSet<string> typeSet,
            bool isAdmin, bool isStaff, bool isDriver, int userId, CancellationToken ct)
        {
            var result = new List<NotificationDto>(limit * 2);

            bool needTopupPending = typeSet.Count == 0 || typeSet.Contains("topup_pending");
            bool needTopupSuccess = typeSet.Count == 0 || typeSet.Contains("topup_success");
            bool needTopupFailed = typeSet.Count == 0 || typeSet.Contains("topup_failed");
            bool needTopupExpired = typeSet.Count == 0 || typeSet.Contains("topup_expired");

            bool needPaymentSucceeded = typeSet.Count == 0 || typeSet.Contains("payment_succeeded");
            bool needPaymentFailed = typeSet.Count == 0 || typeSet.Contains("payment_failed");

            // ---- TopupIntent ----
            if ((needTopupPending || needTopupSuccess || needTopupFailed || needTopupExpired) && (isAdmin || isDriver))
            {
                var tq = _db.TopupIntents.AsNoTracking()
                    .Include(t => t.Wallet)
                    .Where(t => ((t.UpdatedAt != default(DateTime)) ? t.UpdatedAt : t.CreatedAt) >= sinceUtc);

                if (isDriver && !isAdmin)
                    tq = tq.Where(t => t.Wallet.UserId == userId);

                var list = await tq
                    .OrderByDescending(t => (t.UpdatedAt != default(DateTime)) ? t.UpdatedAt : t.CreatedAt)
                    .Take(limit)
                    .Select(t => new
                    {
                        t.Id,
                        t.Status,
                        t.Amount,
                        At = (t.UpdatedAt != default(DateTime)) ? t.UpdatedAt : t.CreatedAt
                    })
                    .ToListAsync(ct);

                foreach (var t in list)
                {
                    string type;
                    string title;
                    string severity = "LOW";

                    if (t.Status == "PENDING")
                    {
                        type = "topup_pending"; title = "Nạp tiền đang xử lý";
                        if (!needTopupPending) continue;
                    }
                    else if (t.Status == "SUCCESS")
                    {
                        type = "topup_success"; title = "Nạp tiền thành công";
                        if (!needTopupSuccess) continue;
                    }
                    else if (t.Status == "FAILED")
                    {
                        type = "topup_failed"; title = "Nạp tiền thất bại";
                        severity = "MEDIUM";
                        if (!needTopupFailed) continue;
                    }
                    else if (t.Status == "EXPIRED")
                    {
                        type = "topup_expired"; title = "Nạp tiền hết hạn";
                        if (!needTopupExpired) continue;
                    }
                    else
                    {
                        type = "topup_pending"; title = "Nạp tiền";
                        if (!needTopupPending) continue;
                    }

                    result.Add(new NotificationDto
                    {
                        Id = "topup:" + t.Id,
                        Title = title,
                        Message = $"Số tiền: {t.Amount:N0}đ",
                        Type = type,
                        Severity = severity,
                        CreatedAt = t.At
                    });
                }
            }

            // ---- Transactions ----
            if ((needPaymentSucceeded || needPaymentFailed) && (isAdmin || isDriver))
            {
                var xq = _db.Transactions.AsNoTracking()
                    .Include(x => x.Wallet)
                    .Where(x => ((x.UpdatedAt != default(DateTime)) ? x.UpdatedAt : x.CreatedAt) >= sinceUtc);

                if (isDriver && !isAdmin)
                    xq = xq.Where(x => x.Wallet.UserId == userId);

                var list = await xq
                    .OrderByDescending(x => (x.UpdatedAt != default(DateTime)) ? x.UpdatedAt : x.CreatedAt)
                    .Take(limit)
                    .Select(x => new
                    {
                        x.Id,
                        x.Type,
                        x.Status,
                        x.Amount,
                        At = (x.UpdatedAt != default(DateTime)) ? x.UpdatedAt : x.CreatedAt
                    })
                    .ToListAsync(ct);

                foreach (var tr in list)
                {
                    string type = (tr.Status == "SUCCEEDED") ? "payment_succeeded" : "payment_failed";
                    if (type == "payment_succeeded" && !needPaymentSucceeded) continue;
                    if (type == "payment_failed" && !needPaymentFailed) continue;

                    string title = (type == "payment_succeeded") ? "Thanh toán thành công" : "Thanh toán thất bại";
                    string severity = (type == "payment_failed") ? "HIGH" : "LOW";

                    result.Add(new NotificationDto
                    {
                        Id = "txn:" + tr.Id,
                        Title = title,
                        Message = $"{tr.Type} • Số tiền: {tr.Amount:N0}đ",
                        Type = type,
                        Severity = severity,
                        CreatedAt = tr.At
                    });
                }
            }

            return result;
        }

        // ===== Helpers local (ngắn gọn, không phải helper method tái sử dụng) =====
        private static string NormalizeSeverity(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "LOW";
            var up = s.Trim().ToUpperInvariant();
            return (up == "CRITICAL" || up == "HIGH" || up == "MEDIUM" || up == "LOW") ? up : "LOW";
        }

        private static int SeverityRank(string sev)
        {
            if (sev == "CRITICAL") return 4;
            if (sev == "HIGH") return 3;
            if (sev == "MEDIUM") return 2;
            return 1; // LOW
        }

        private static string MapPriorityToSeverity(string? prio)
        {
            if (string.IsNullOrWhiteSpace(prio)) return "LOW";
            var up = prio.Trim().ToUpperInvariant();
            if (up == "CRITICAL") return "CRITICAL";
            if (up == "HIGH") return "HIGH";
            if (up == "MEDIUM") return "MEDIUM";
            return "LOW";
        }
    }
}
