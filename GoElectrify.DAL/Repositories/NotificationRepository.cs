using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Dto.Notification;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _db;
        public NotificationRepository(AppDbContext db) => _db = db;

        // ---- Helpers (labels & keys) ----
        private static string BookingTitle(string status) => status switch
        {
            "CONFIRMED" => "Đặt chỗ xác nhận thành công",
            "FAILED" => "Đặt chỗ thất bại",
            "CANCELED" => "Đặt chỗ đã huỷ",
            "EXPIRED" => "Đặt chỗ hết hạn",
            _ => "Đặt chỗ mới"
        };
        private static string BookingSeverity(string status) => status == "FAILED" ? "HIGH" : "LOW";
        private static (string title, string severity) TxLabel(string type, string status) => status switch
        {
            "SUCCESS" => ($"{type} thành công", "LOW"),
            "FAILED" => ($"{type} thất bại", "HIGH"),
            "REFUNDED" => ($"{type} hoàn tiền", "MEDIUM"),
            _ => ($"{type} cập nhật", "LOW")
        };

        private static string ChargingTitle(string s) => s switch
        {
            "PENDING" => "Phiên sạc đang chờ",
            "RUNNING" => "Đang sạc",
            "STOPPED" => "Đã dừng sạc",
            "COMPLETED" => "Hoàn tất phiên sạc",
            "FAILED" => "Phiên sạc thất bại",
            _ => "Cập nhật phiên sạc"
        };
        private static string ChargingSeverity(string s) => s switch
        {
            "FAILED" => "HIGH",
            "STOPPED" => "MEDIUM",
            _ => "LOW"
        };
        private static DateTime PickAt(DateTime? endedAt, DateTime startedAt, DateTime? updatedAt, DateTime createdAt)
            => endedAt ?? (startedAt != default ? startedAt : (updatedAt ?? createdAt));

        private static string KeyAssign(int id, DateTime? at) => $"assign:{id}:{(at ?? DateTime.UnixEpoch).Ticks}";
        private static string KeyRevoke(int id, DateTime at) => $"revoke:{id}:{at.Ticks}";
        private static string KeyBooking(int id) => $"booking:{id}";
        private static string KeyTx(int id) => $"tx:{id}";
        private static string KeyDeposit(int id) => $"deposit:{id}";
        private static string KeyIncident(int id) => $"incident:{id}";
        private static string KeyCharging(int id) => $"charging:{id}";



        // ===================== DASHBOARD =====================

        public async Task<List<NotificationDto>> GetByRoleAsync(int userId, string role, CancellationToken ct)
        {
            var since = DateTime.UtcNow.AddDays(-7);
            var isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            var isStaff = role.Equals("Staff", StringComparison.OrdinalIgnoreCase);

            var bag = new List<NotificationDto>(256);

            // Bookings
            var bkQuery = _db.Bookings.AsNoTracking()
                .Where(b => b.CreatedAt >= since || b.UpdatedAt >= since);
            if (!isAdmin) bkQuery = bkQuery.Where(b => b.UserId == userId);

            var bkRows = await bkQuery
                .Select(b => new { b.Id, b.Status, StationName = b.Station.Name, At = b.UpdatedAt })
                .ToListAsync(ct);

            foreach (var b in bkRows)
            {
                var status = b.Status ?? "UPDATED";
                bag.Add(new NotificationDto
                {
                    Id = KeyBooking(b.Id),
                    Type = $"booking.{status.ToLowerInvariant()}",
                    Title = BookingTitle(status),
                    Message = $"Trạm {b.StationName}",
                    Severity = BookingSeverity(status),
                    CreatedAt = b.At
                });
            }

            // Charging sessions (PENDING, RUNNING, STOPPED, COMPLETED, FAILED) 
            var cs = _db.Set<ChargingSession>().AsNoTracking()
                .Where(s =>
                    (s.EndedAt != null && s.EndedAt >= since) ||
                    (s.StartedAt >= since) ||
                    (s.UpdatedAt != null && s.UpdatedAt >= since) ||
                     s.CreatedAt >= since);

            if (!isAdmin)
            {
                // User chỉ thấy session liên quan booking của chính họ
                cs = cs.Where(s => s.Booking != null && s.Booking!.UserId == userId);
            }

            var csRows = await cs
                .Select(s => new
                {
                    s.Id,
                    s.Status,
                    s.EnergyKwh,
                    s.Cost,
                    s.CreatedAt,
                    s.UpdatedAt,
                    s.StartedAt,
                    s.EndedAt,
                    StationName = s.Charger.Station.Name
                })
                .ToListAsync(ct);

            foreach (var s in csRows)
            {
                var status = string.IsNullOrWhiteSpace(s.Status) ? "RUNNING" : s.Status!;
                var at = PickAt(s.EndedAt, s.StartedAt, s.UpdatedAt, s.CreatedAt);

                var msg = s.StationName ?? "";
                if (status == "COMPLETED")
                    msg = $"{s.StationName} • {s.EnergyKwh:N2} kWh • {s.Cost?.ToString("n0") ?? "0"}đ";
                else if (status == "STOPPED")
                    msg = $"{s.StationName} • Phiên đã dừng";
                else if (status == "FAILED")
                    msg = $"{s.StationName} • Đã xảy ra lỗi";

                bag.Add(new NotificationDto
                {
                    Id = KeyCharging(s.Id),
                    Type = $"charging.{status.ToLowerInvariant()}",
                    Title = ChargingTitle(status),
                    Message = msg,
                    Severity = ChargingSeverity(status),
                    CreatedAt = at
                });
            }

            // Transactions
            var txQuery = _db.Transactions.AsNoTracking()
                .Where(t => t.CreatedAt >= since);
            if (!isAdmin) txQuery = txQuery.Where(t => t.Wallet.UserId == userId);

            var txRows = await txQuery
                .Select(t => new { t.Id, t.Type, t.Status, t.Amount, t.CreatedAt })
                .ToListAsync(ct);

            foreach (var t in txRows)
            {
                var type = string.IsNullOrWhiteSpace(t.Type) ? "Giao dịch" : t.Type!;
                var status = string.IsNullOrWhiteSpace(t.Status) ? "UPDATED" : t.Status!;
                var (title, severity) = TxLabel(type, status);

                bag.Add(new NotificationDto
                {
                    Id = KeyTx(t.Id),
                    Type = $"transaction.{type.ToLowerInvariant()}.{status.ToLowerInvariant()}",
                    Title = title,
                    Message = $"{t.Amount:n0}đ",
                    Severity = severity,
                    CreatedAt = t.CreatedAt
                });
            }

            // Staff-specific
            if (isStaff)
            {
                var myStationIds = await _db.StationStaff.AsNoTracking()
                    .Where(s => s.UserId == userId && s.RevokedAt == null)
                    .Select(s => s.StationId)
                    .ToListAsync(ct);

                // Incidents tại trạm của mình
                var incs = await _db.Incidents.AsNoTracking()
                    .Where(i => myStationIds.Contains(i.StationId) && i.CreatedAt >= since)
                    .Select(i => new { i.Id, StationName = i.Station.Name, i.Priority, i.CreatedAt })
                    .ToListAsync(ct);

                foreach (var i in incs)
                {
                    bag.Add(new NotificationDto
                    {
                        Id = KeyIncident(i.Id),
                        Type = "incident.reported",
                        Title = "Sự cố trạm được báo cáo",
                        Message = $"Trạm {i.StationName}",
                        Severity = string.IsNullOrWhiteSpace(i.Priority) ? "MEDIUM" : i.Priority!,
                        CreatedAt = i.CreatedAt
                    });
                }

                // Deposit (nạp hộ): PostgreSQL → ILike
                var deps = await _db.Transactions.AsNoTracking()
                    .Where(t => t.CreatedAt >= since &&
                                t.Note != null &&
                                EF.Functions.ILike(t.Note!, $"%STAFF_DEPOSIT:{userId}%"))
                    .Select(t => new { t.Id, t.Status, t.Amount, t.CreatedAt })
                    .ToListAsync(ct);

                foreach (var d in deps)
                {
                    var ok = string.Equals(d.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase);
                    bag.Add(new NotificationDto
                    {
                        Id = KeyDeposit(d.Id),
                        Type = $"wallet.staffdeposit.{(d.Status ?? "UPDATED").ToLowerInvariant()}",
                        Title = ok ? "Nạp hộ khách thành công" : "Nạp hộ khách thất bại",
                        Message = $"{d.Amount:n0}đ",
                        Severity = ok ? "LOW" : "HIGH",
                        CreatedAt = d.CreatedAt
                    });
                }

                // Revoke (quyền của chính mình)
                var revs = await _db.StationStaff.AsNoTracking()
                    .Where(s => s.UserId == userId && s.RevokedAt != null && s.RevokedAt >= since)
                    .Select(s => new { s.Id, s.Station.Name, s.RevokedAt, s.RevokedReason })
                    .ToListAsync(ct);

                foreach (var s in revs)
                {
                    var at = s.RevokedAt!.Value;
                    var reason = string.IsNullOrWhiteSpace(s.RevokedReason)
                        ? "Quyền của bạn tại trạm đã bị thu hồi."
                        : s.RevokedReason!;
                    bag.Add(new NotificationDto
                    {
                        Id = KeyRevoke(s.Id, at),
                        Type = "station.staff.revoked",
                        Title = "Bạn bị thu hồi quyền tại trạm",
                        Message = $"Trạm {s.Name} • {reason}",
                        Severity = "MEDIUM",
                        CreatedAt = at
                    });
                }
            }

            // Admin-specific
            if (isAdmin)
            {
                var incs = await _db.Incidents.AsNoTracking()
                    .Where(i => i.CreatedAt >= since)
                    .Select(i => new { i.Id, StationName = i.Station.Name, i.Priority, i.CreatedAt })
                    .ToListAsync(ct);

                foreach (var i in incs)
                {
                    bag.Add(new NotificationDto
                    {
                        Id = KeyIncident(i.Id),
                        Type = "incident.reported",
                        Title = "Sự cố mới toàn hệ thống",
                        Message = $"Trạm {i.StationName}",
                        Severity = string.IsNullOrWhiteSpace(i.Priority) ? "MEDIUM" : i.Priority!,
                        CreatedAt = i.CreatedAt
                    });
                }

                var asgs = await _db.StationStaff.AsNoTracking()
                    .Where(s => s.AssignedAt != null && s.AssignedAt >= since)
                    .Select(s => new { s.Id, s.Station.Name, s.AssignedAt })
                    .ToListAsync(ct);

                foreach (var s in asgs)
                {
                    bag.Add(new NotificationDto
                    {
                        Id = KeyAssign(s.Id, s.AssignedAt),
                        Type = "station.staff.assigned",
                        Title = "Gán nhân viên vào trạm",
                        Message = $"Trạm {s.Name}",
                        Severity = "LOW",
                        CreatedAt = s.AssignedAt!
                    });
                }

                var revs = await _db.StationStaff.AsNoTracking()
                    .Where(s => s.RevokedAt != null && s.RevokedAt >= since)
                    .Select(s => new { s.Id, s.Station.Name, s.RevokedAt })
                    .ToListAsync(ct);

                foreach (var s in revs)
                {
                    var at = s.RevokedAt!.Value;
                    bag.Add(new NotificationDto
                    {
                        Id = KeyRevoke(s.Id, at),
                        Type = "station.staff.revoked",
                        Title = "Thu hồi quyền nhân viên tại trạm",
                        Message = $"Trạm {s.Name}",
                        Severity = "MEDIUM",
                        CreatedAt = at
                    });
                }
            }

            return bag.OrderByDescending(x => x.CreatedAt).ToList();
        }

        // ================== ID VISIBILITY & ALL IDS ==================

        public async Task<List<string>> GetAllIdsAsync(int userId, string role, CancellationToken ct)
        {
            var isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            var isStaff = role.Equals("Staff", StringComparison.OrdinalIgnoreCase);

            var ids = new List<string>(256);

            // bookings
            var bQuery = _db.Bookings.AsNoTracking();
            if (!isAdmin) bQuery = bQuery.Where(b => b.UserId == userId);
            foreach (var id in await bQuery.Select(b => b.Id).ToListAsync(ct))
                ids.Add(KeyBooking(id));

            // charging (lọc theo booking.UserId cho non-admin)
            var cQuery = _db.Set<ChargingSession>().AsNoTracking();
            if (!isAdmin) cQuery = cQuery.Where(s => s.Booking != null && s.Booking!.UserId == userId);
            foreach (var id in await cQuery.Select(s => s.Id).ToListAsync(ct))
                ids.Add(KeyCharging(id));

            // transactions
            var tQuery = _db.Transactions.AsNoTracking();
            if (!isAdmin) tQuery = tQuery.Where(t => t.Wallet.UserId == userId);
            foreach (var id in await tQuery.Select(t => t.Id).ToListAsync(ct))
                ids.Add(KeyTx(id));

            if (isAdmin)
            {
                foreach (var id in await _db.Incidents.AsNoTracking().Select(i => i.Id).ToListAsync(ct))
                    ids.Add(KeyIncident(id));

                var assigns = await _db.StationStaff.AsNoTracking()
                    .Where(s => s.AssignedAt != null)
                    .Select(s => new { s.Id, s.AssignedAt })
                    .ToListAsync(ct);
                ids.AddRange(assigns.Select(a => KeyAssign(a.Id, a.AssignedAt)));

                var revokes = await _db.StationStaff.AsNoTracking()
                    .Where(s => s.RevokedAt != null)
                    .Select(s => new { s.Id, s.RevokedAt })
                    .ToListAsync(ct);
                ids.AddRange(revokes.Select(r => KeyRevoke(r.Id, r.RevokedAt!.Value)));

                return ids.Distinct().ToList();
            }

            if (isStaff)
            {
                var myStationIds = await _db.StationStaff.AsNoTracking()
                    .Where(s => s.UserId == userId && s.RevokedAt == null)
                    .Select(s => s.StationId)
                    .ToListAsync(ct);

                foreach (var id in await _db.Incidents.AsNoTracking()
                    .Where(i => myStationIds.Contains(i.StationId))
                    .Select(i => i.Id).ToListAsync(ct))
                    ids.Add(KeyIncident(id));

                foreach (var id in await _db.Transactions.AsNoTracking()
                    .Where(t => t.Note != null && EF.Functions.ILike(t.Note!, $"%STAFF_DEPOSIT:{userId}%"))
                    .Select(t => t.Id).ToListAsync(ct))
                    ids.Add(KeyDeposit(id));

                var revs = await _db.StationStaff.AsNoTracking()
                    .Where(s => s.UserId == userId && s.RevokedAt != null)
                    .Select(s => new { s.Id, s.RevokedAt })
                    .ToListAsync(ct);
                ids.AddRange(revs.Select(r => KeyRevoke(r.Id, r.RevokedAt!.Value)));
            }

            return ids.Distinct().ToList();
        }

        public async Task<bool> IsVisibleIdAsync(int userId, string role, string notifId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(notifId)) return false;

            var p = notifId.IndexOf(':');
            if (p <= 0) return false;
            var prefix = notifId[..p];
            var rest = notifId[(p + 1)..];

            var q = rest.IndexOf(':');
            var idStr = q >= 0 ? rest[..q] : rest;
            if (!int.TryParse(idStr, out var entityId)) return false;

            var isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            var isStaff = role.Equals("Staff", StringComparison.OrdinalIgnoreCase);

            switch (prefix)
            {
                case "booking":
                    return isAdmin
                        ? await _db.Bookings.AsNoTracking().AnyAsync(b => b.Id == entityId, ct)
                        : await _db.Bookings.AsNoTracking().AnyAsync(b => b.Id == entityId && b.UserId == userId, ct);

                case "charging": // kiểm tra theo Booking.UserId cho non-admin
                    return isAdmin
                        ? await _db.Set<ChargingSession>().AsNoTracking().AnyAsync(s => s.Id == entityId, ct)
                        : await _db.Set<ChargingSession>().AsNoTracking()
                            .AnyAsync(s => s.Id == entityId && s.Booking != null && s.Booking!.UserId == userId, ct);

                case "tx":
                    return isAdmin
                        ? await _db.Transactions.AsNoTracking().AnyAsync(t => t.Id == entityId, ct)
                        : await _db.Transactions.AsNoTracking().AnyAsync(t => t.Id == entityId && t.Wallet.UserId == userId, ct);

                case "incident":
                    if (isAdmin) return await _db.Incidents.AsNoTracking().AnyAsync(i => i.Id == entityId, ct);
                    if (!isStaff) return false;
                    var myStationIds = await _db.StationStaff.AsNoTracking()
                        .Where(s => s.UserId == userId && s.RevokedAt == null)
                        .Select(s => s.StationId).ToListAsync(ct);
                    return await _db.Incidents.AsNoTracking()
                        .AnyAsync(i => i.Id == entityId && myStationIds.Contains(i.StationId), ct);

                case "deposit":
                    if (!isStaff) return false;
                    return await _db.Transactions.AsNoTracking()
                        .AnyAsync(t => t.Id == entityId &&
                                       t.Note != null &&
                                       EF.Functions.ILike(t.Note!, $"%STAFF_DEPOSIT:{userId}%"), ct);

                case "assign":
                    return isAdmin && await _db.StationStaff.AsNoTracking().AnyAsync(s => s.Id == entityId, ct);

                case "revoke":
                    if (isAdmin) return await _db.StationStaff.AsNoTracking().AnyAsync(s => s.Id == entityId, ct);
                    if (isStaff) return await _db.StationStaff.AsNoTracking().AnyAsync(s => s.Id == entityId && s.UserId == userId, ct);
                    return false;

                default:
                    return false;
            }
        }

        // ===================== STATE TABLE =====================

        public Task<DateTime?> GetLastSeenUtcAsync(int userId, CancellationToken ct)
            => _db.Notifications.AsNoTracking()
                .Where(n => n.UserId == userId
                            && n.IsMarker
                            && n.MarkerKind == "LAST_SEEN"
                            && n.MarkerValueUtc != null)
                .Select(n => n.MarkerValueUtc)
                .MaxAsync(ct);


        public async Task UpsertLastSeenUtcAsync(int userId, DateTime lastSeenUtc, CancellationToken ct)
        {
            // Lấy tất cả marker LAST_SEEN của user (nếu lỡ có nhiều do lịch sử)
            var markers = await _db.Notifications
                .Where(n => n.UserId == userId && n.IsMarker && n.MarkerKind == "LAST_SEEN")
                .OrderByDescending(n => n.Id) // ưu tiên row mới nhất
                .ToListAsync(ct);

            if (markers.Count == 0)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = userId,
                    IsMarker = true,
                    MarkerKind = "LAST_SEEN",
                    MarkerValueUtc = lastSeenUtc,
                    NotifKey = "" // giữ nguyên contract cũ
                });
                await _db.SaveChangesAsync(ct);
                return;
            }

            // Cập nhật row mới nhất
            var keep = markers[0];
            keep.MarkerValueUtc = lastSeenUtc;

            // Dọn duplicates cũ (không cần đổi schema)
            if (markers.Count > 1)
            {
                var extras = markers.Skip(1).ToList();
                _db.Notifications.RemoveRange(extras);
            }

            await _db.SaveChangesAsync(ct);
        }


        public async Task<HashSet<string>> GetReadKeysAsync(int userId, CancellationToken ct)
            => (await _db.Notifications.AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsMarker && n.ReadAtUtc != null)
                .Select(n => n.NotifKey)
                .ToListAsync(ct)).ToHashSet();

        public async Task MarkReadAsync(int userId, string notifKey, CancellationToken ct)
        {
            var row = await _db.Notifications
                .FirstOrDefaultAsync(n => n.UserId == userId && !n.IsMarker && n.NotifKey == notifKey, ct);

            if (row is null)
            {
                _db.Notifications.Add(new Notification
                {
                    UserId = userId,
                    IsMarker = false,
                    NotifKey = notifKey,
                    ReadAtUtc = DateTime.UtcNow
                });
            }
            else if (row.ReadAtUtc is null)
            {
                row.ReadAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
        }

        public async Task MarkAllReadAsync(int userId, IEnumerable<string> notifKeys, CancellationToken ct)
        {
            var keys = notifKeys?.Distinct().ToList() ?? [];
            if (keys.Count == 0) return;

            var existing = await _db.Notifications.AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsMarker && keys.Contains(n.NotifKey))
                .Select(n => n.NotifKey)
                .ToListAsync(ct);
            var exist = existing.ToHashSet();

            var toInsert = keys.Where(k => !exist.Contains(k))
                .Select(k => new Notification
                {
                    UserId = userId,
                    IsMarker = false,
                    NotifKey = k,
                    ReadAtUtc = DateTime.UtcNow
                }).ToList();

            if (toInsert.Count > 0)
                _db.Notifications.AddRange(toInsert);

            await _db.Notifications
                .Where(n => n.UserId == userId && !n.IsMarker && keys.Contains(n.NotifKey) && n.ReadAtUtc == null)
                .ExecuteUpdateAsync(u => u.SetProperty(n => n.ReadAtUtc, _ => DateTime.UtcNow), ct);

            if (toInsert.Count > 0)
                await _db.SaveChangesAsync(ct);
        }
    }
}
