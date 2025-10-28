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

            // Severity min (inline normalize + rank)
            string minSeverity = (query.MinSeverity ?? "LOW").Trim().ToUpperInvariant();
            if (minSeverity != "CRITICAL" && minSeverity != "HIGH" && minSeverity != "MEDIUM" && minSeverity != "LOW")
                minSeverity = "LOW";
            int minRank = minSeverity == "CRITICAL" ? 4 : minSeverity == "HIGH" ? 3 : minSeverity == "MEDIUM" ? 2 : 1;

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

            // ---------- PAYMENT (Topup + Subscription Purchase) ----------
            bucket.AddRange(await BuildPaymentAsync(
                sinceUtc, limit, typeSet, isAdmin, isStaff, isDriver, userId, cancellationToken));

            // ===== 4) Áp MinSeverity + Sort + Limit (inline rank) =====
            List<NotificationDto> filtered = new List<NotificationDto>(bucket.Count);
            foreach (var n in bucket)
            {
                string sev = (n.Severity ?? "LOW").Trim().ToUpperInvariant();
                if (sev != "CRITICAL" && sev != "HIGH" && sev != "MEDIUM" && sev != "LOW")
                    sev = "LOW";
                int rank = sev == "CRITICAL" ? 4 : sev == "HIGH" ? 3 : sev == "MEDIUM" ? 2 : 1;
                if (rank >= minRank) filtered.Add(n);
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
            var result = new List<NotificationDto>(limit * 4);

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

            // ---- BOOKING DEPOSIT (BOOKING_FEE thuộc Booking) ----
            // Hiển thị đặt cọc tại nhóm Booking (không đưa sang Payment).
            // Nếu chưa join Transaction -> Booking/Station, tạm ẩn với Staff để tránh lộ phạm vi.
            if (typeSet.Count == 0
                || typeSet.Contains("booking_deposit_succeeded")
                || typeSet.Contains("booking_deposit_failed"))
            {
                var dq = _db.Transactions.AsNoTracking()
                    .Include(x => x.Wallet)
                    .Where(x =>
                        x.Type == "BOOKING_FEE" &&
                        (((x.UpdatedAt != default(DateTime)) ? x.UpdatedAt : x.CreatedAt) >= sinceUtc));

                if (isDriver && !isAdmin)
                    dq = dq.Where(x => x.Wallet.UserId == userId);

                if (isStaff && !isAdmin)
                    dq = dq.Where(_ => false); // Ẩn với Staff nếu chưa link tới Station/Booking

                var dlist = await dq
                    .OrderByDescending(x => (x.UpdatedAt != default(DateTime)) ? x.UpdatedAt : x.CreatedAt)
                    .Take(limit)
                    .Select(x => new
                    {
                        x.Id,
                        x.Status,
                        x.Amount,
                        At = (x.UpdatedAt != default(DateTime)) ? x.UpdatedAt : x.CreatedAt
                    })
                    .ToListAsync(ct);

                foreach (var tr in dlist)
                {
                    bool ok = tr.Status == "SUCCEEDED";
                    string nType = ok ? "booking_deposit_succeeded" : "booking_deposit_failed";
                    if (typeSet.Count > 0 && !typeSet.Contains(nType)) continue;

                    result.Add(new NotificationDto
                    {
                        Id = "deposit:" + tr.Id,
                        Title = ok ? "Đặt cọc thành công" : "Đặt cọc thất bại",
                        Message = $"Số tiền: {tr.Amount:N0}đ",
                        Type = nType,
                        Severity = ok ? "LOW" : "HIGH",
                        CreatedAt = tr.At
                    });
                }
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
                // inline map priority -> severity
                string sev = (i.Priority ?? "LOW").Trim().ToUpperInvariant();
                if (sev != "CRITICAL" && sev != "HIGH" && sev != "MEDIUM" && sev != "LOW")
                    sev = "LOW";

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
        // Admin: mọi ví; Driver: ví của mình; Staff: HIỂN THỊ TOPUP (nạp ví) cho khách
        // Chỉ hiển thị: TopupIntents (nạp ví) + Mua gói (SUBSCRIPTION_PURCHASE)
        // KHÔNG hiển thị BOOKING_FEE (đặt cọc) bên Payment — đã chuyển qua Booking.
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

            // ---- TopupIntents (Nạp ví) — Admin, Driver, Staff đều thấy ----
            if ((needTopupPending || needTopupSuccess || needTopupFailed || needTopupExpired)
                && (isAdmin || isDriver || isStaff))
            {
                var tq = _db.TopupIntents.AsNoTracking()
                    .Include(t => t.Wallet)
                    .Where(t => (((t.UpdatedAt != default(DateTime)) ? t.UpdatedAt : t.CreatedAt) >= sinceUtc));

                if (isDriver && !isAdmin)
                {
                    // Driver: chỉ thấy ví của chính mình
                    tq = tq.Where(t => t.Wallet.UserId == userId);
                }
                // Staff: hiển thị tất cả topup để phục vụ nạp hộ cho khách.
                // Nếu có trường CreatedByUserId/HandledByUserId thì filter tại đây.

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

            // ---- Transactions: CHỈ mua gói (SUBSCRIPTION_PURCHASE) ----
            // Admin & Driver xem; Staff tạm ẩn (thêm || isStaff nếu muốn staff nhìn thấy).
            if ((needPaymentSucceeded || needPaymentFailed) && (isAdmin || isDriver))
            {
                string[] subscriptionTypes = new[] { "SUBSCRIPTION_PURCHASE" }; // đổi literal nếu khác

                var subQ = _db.Transactions.AsNoTracking()
                    .Include(x => x.Wallet)
                    .Where(x =>
                        subscriptionTypes.Contains(x.Type) &&
                        (((x.UpdatedAt != default(DateTime)) ? x.UpdatedAt : x.CreatedAt) >= sinceUtc));

                if (isDriver && !isAdmin)
                    subQ = subQ.Where(x => x.Wallet.UserId == userId);

                var subList = await subQ
                    .OrderByDescending(x => (x.UpdatedAt != default(DateTime)) ? x.UpdatedAt : x.CreatedAt)
                    .Take(limit)
                    .Select(x => new
                    {
                        x.Id,
                        x.Status,
                        x.Amount,
                        At = (x.UpdatedAt != default(DateTime)) ? x.UpdatedAt : x.CreatedAt
                    })
                    .ToListAsync(ct);

                foreach (var tr in subList)
                {
                    bool ok = tr.Status == "SUCCEEDED";
                    string nType = ok ? "subscription_purchased" : "subscription_purchase_failed";
                    if (ok && !needPaymentSucceeded) continue;
                    if (!ok && !needPaymentFailed) continue;

                    result.Add(new NotificationDto
                    {
                        Id = "sub:" + tr.Id,
                        Title = ok ? "Mua gói thành công" : "Mua gói thất bại",
                        Message = $"Số tiền: {tr.Amount:N0}đ",
                        Type = nType,
                        Severity = ok ? "LOW" : "HIGH",
                        CreatedAt = tr.At
                    });
                }
            }

            return result;
        }
    }
}
