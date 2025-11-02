using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Dto.Notification;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly AppDbContext _db;
        public NotificationRepository(AppDbContext db) => _db = db;

        public async Task<List<NotificationDto>> GetByRoleAsync(int userId, string role, CancellationToken ct)
        {
            var since = DateTime.UtcNow.AddDays(-7);
            var bag = new List<NotificationDto>();

            bool isDriver = role.Equals("Driver", StringComparison.OrdinalIgnoreCase);
            bool isStaff = role.Equals("Staff", StringComparison.OrdinalIgnoreCase);
            bool isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

            // === BOOKING + TRANSACTION cho tất cả role (lọc theo user nếu không phải admin) ===
            // Booking
            var bookingQuery = _db.Bookings.Include(b => b.Station)
                .AsQueryable();

            bookingQuery = isAdmin
                ? bookingQuery.Where(b => b.CreatedAt >= since || b.UpdatedAt >= since)
                : bookingQuery.Where(b => b.UserId == userId && (b.CreatedAt >= since || b.UpdatedAt >= since));

            var bookings = await bookingQuery.ToListAsync(ct);
            foreach (var b in bookings)
            {
                var title = b.Status switch
                {
                    "CONFIRMED" => "Đặt chỗ xác nhận thành công",
                    "FAILED" => "Đặt chỗ thất bại",
                    "CANCELED" => "Đặt chỗ đã huỷ",
                    "EXPIRED" => "Đặt chỗ hết hạn",
                    _ => "Đặt chỗ mới"
                };

                bag.Add(new NotificationDto
                {
                    Id = $"booking:{b.Id}",
                    Type = $"booking.{b.Status.ToLower()}",
                    Title = title,
                    Message = $"Trạm {b.Station.Name}",
                    Severity = b.Status == "FAILED" ? "HIGH" : "LOW",
                    CreatedAt = b.UpdatedAt
                });
            }

            // Transactions (payment/wallet/subscription)
            var txQuery = _db.Transactions.Include(t => t.Wallet).AsQueryable();

            txQuery = isAdmin
                ? txQuery.Where(t => t.CreatedAt >= since)
                : txQuery.Where(t => t.Wallet.UserId == userId && t.CreatedAt >= since);

            var txs = await txQuery.ToListAsync(ct);
            foreach (var t in txs)
            {
                var title = t.Status switch
                {
                    "SUCCESS" => $"{t.Type} thành công",
                    "FAILED" => $"{t.Type} thất bại",
                    "REFUNDED" => $"{t.Type} hoàn tiền",
                    _ => $"{t.Type} cập nhật"
                };
                var sev = t.Status switch
                {
                    "FAILED" => "HIGH",
                    "REFUNDED" => "MEDIUM",
                    _ => "LOW"
                };

                bag.Add(new NotificationDto
                {
                    Id = $"tx:{t.Id}",
                    Type = $"transaction.{t.Type.ToLower()}.{t.Status.ToLower()}",
                    Title = title,
                    Message = $"{t.Type} • {t.Amount:n0}đ",
                    Severity = sev,
                    CreatedAt = t.CreatedAt
                });
            }

            // === STAFF đặc thù: Incident trạm mình + Deposit nạp hộ + Revoked quyền ===
            if (isStaff)
            {
                // Incident trạm mình (đang active)
                var stationIds = await _db.StationStaff
                    .Where(s => s.UserId == userId && s.RevokedAt == null)
                    .Select(s => s.StationId)
                    .ToListAsync(ct);

                var incidents = await _db.Incidents
                    .Include(i => i.Station)
                    .Where(i => stationIds.Contains(i.StationId) && i.CreatedAt >= since)
                    .ToListAsync(ct);

                foreach (var i in incidents)
                {
                    bag.Add(new NotificationDto
                    {
                        Id = $"incident:{i.Id}",
                        Type = "incident.reported",
                        Title = "Sự cố trạm được báo cáo",
                        Message = $"Trạm {i.Station.Name}",
                        Severity = i.Priority ?? "MEDIUM",
                        CreatedAt = i.CreatedAt
                    });
                }

                // Deposit: staff nạp hộ khách (STAFF_DEPOSIT:{staffUserId})
                var deposits = await _db.Transactions
                    .Where(t => t.Note != null
                                && EF.Functions.Like(t.Note, $"%STAFF_DEPOSIT:{userId}%")
                                && t.CreatedAt >= since)
                    .ToListAsync(ct);

                foreach (var t in deposits)
                {
                    bag.Add(new NotificationDto
                    {
                        Id = $"deposit:{t.Id}",
                        Type = $"wallet.staffdeposit.{t.Status.ToLower()}",
                        Title = t.Status == "SUCCESS" ? "Nạp hộ khách thành công" : "Nạp hộ khách thất bại",
                        Message = $"{t.Amount:n0}đ",
                        Severity = t.Status == "FAILED" ? "HIGH" : "LOW",
                        CreatedAt = t.CreatedAt
                    });
                }

                // Staff bị thu quyền (event-based)
                var revokedMine = await _db.StationStaff
                    .Include(s => s.Station)
                    .Where(s => s.UserId == userId && s.RevokedAt != null && s.RevokedAt >= since)
                    .ToListAsync(ct);

                foreach (var s in revokedMine)
                {
                    var reason = string.IsNullOrWhiteSpace(s.RevokedReason)
                        ? "Quyền của bạn tại trạm đã bị thu hồi."
                        : s.RevokedReason!;
                    var at = s.RevokedAt!.Value;

                    bag.Add(new NotificationDto
                    {
                        Id = $"revoke:{s.Id}:{at.Ticks}",
                        Type = "station.staff.revoked",
                        Title = "Bạn bị thu hồi quyền tại trạm",
                        Message = $"Trạm {s.Station.Name} • {reason}",
                        Severity = "MEDIUM",
                        CreatedAt = at
                    });
                }
            }

            // === ADMIN: incident hệ thống + assign/revoke (event-based) ===
            if (isAdmin)
            {
                var incidents = await _db.Incidents.Include(i => i.Station)
                    .Where(i => i.CreatedAt >= since)
                    .ToListAsync(ct);

                foreach (var i in incidents)
                {
                    bag.Add(new NotificationDto
                    {
                        Id = $"incident:{i.Id}",
                        Type = "incident.reported",
                        Title = "Sự cố mới toàn hệ thống",
                        Message = $"Trạm {i.Station.Name}",
                        Severity = i.Priority ?? "MEDIUM",
                        CreatedAt = i.CreatedAt
                    });
                }

                var assigns = await _db.StationStaff
                    .Include(s => s.Station)
                    .Where(s => s.AssignedAt >= since)
                    .ToListAsync(ct);

                foreach (var s in assigns)
                {
                    var at = s.AssignedAt;
                    bag.Add(new NotificationDto
                    {
                        Id = $"assign:{s.Id}:{at.Ticks}",
                        Type = "station.staff.assigned",
                        Title = "Gán nhân viên vào trạm",
                        Message = $"Trạm {s.Station.Name}",
                        Severity = "LOW",
                        CreatedAt = at
                    });
                }

                var revokedAll = await _db.StationStaff
                    .Include(s => s.Station)
                    .Where(s => s.RevokedAt != null && s.RevokedAt >= since)
                    .ToListAsync(ct);

                foreach (var s in revokedAll)
                {
                    var at = s.RevokedAt!.Value;
                    bag.Add(new NotificationDto
                    {
                        Id = $"revoke:{s.Id}:{at.Ticks}",
                        Type = "station.staff.revoked",
                        Title = "Thu hồi quyền nhân viên tại trạm",
                        Message = $"Trạm {s.Station.Name}",
                        Severity = "MEDIUM",
                        CreatedAt = at
                    });
                }
            }

            return bag.OrderByDescending(x => x.CreatedAt).ToList();
        }

        public async Task<List<string>> GetAllIdsAsync(int userId, string role, CancellationToken ct)
        {
            var ids = new List<string>();
            bool isDriver = role.Equals("Driver", StringComparison.OrdinalIgnoreCase);
            bool isStaff = role.Equals("Staff", StringComparison.OrdinalIgnoreCase);
            bool isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

            // Booking + Transaction: admin = tất cả; staff/driver = của mình
            {
                var bookingIds = isAdmin
                    ? await _db.Bookings.Select(b => $"booking:{b.Id}").ToListAsync(ct)
                    : await _db.Bookings.Where(b => b.UserId == userId)
                        .Select(b => $"booking:{b.Id}").ToListAsync(ct);
                ids.AddRange(bookingIds);

                var txIds = isAdmin
                    ? await _db.Transactions.Select(t => $"tx:{t.Id}").ToListAsync(ct)
                    : await _db.Transactions.Include(t => t.Wallet)
                        .Where(t => t.Wallet.UserId == userId)
                        .Select(t => $"tx:{t.Id}").ToListAsync(ct);
                ids.AddRange(txIds);
            }

            // Staff: deposit của mình
            if (isStaff)
            {
                var depositIds = await _db.Transactions
                    .Where(t => t.Note != null && EF.Functions.Like(t.Note, $"%STAFF_DEPOSIT:{userId}%"))
                    .Select(t => $"deposit:{t.Id}")
                    .ToListAsync(ct);
                ids.AddRange(depositIds);
            }

            // Incident: admin = tất cả; staff = trạm active của mình
            if (isAdmin)
            {
                var incidentIds = await _db.Incidents.Select(i => $"incident:{i.Id}").ToListAsync(ct);
                ids.AddRange(incidentIds);
            }
            else if (isStaff)
            {
                var stationIds = await _db.StationStaff
                    .Where(s => s.UserId == userId && s.RevokedAt == null)
                    .Select(s => s.StationId).ToListAsync(ct);

                var incidentIds = await _db.Incidents
                    .Where(i => stationIds.Contains(i.StationId))
                    .Select(i => $"incident:{i.Id}")
                    .ToListAsync(ct);
                ids.AddRange(incidentIds);
            }

            // Event-based: revoke/assign (admin tất cả; staff chỉ revoke của mình)
            if (isAdmin)
            {
                var assignIds = await _db.StationStaff
                    .Select(s => $"assign:{s.Id}:{s.AssignedAt.Ticks}")
                    .ToListAsync(ct);
                ids.AddRange(assignIds);

                var revokeIds = await _db.StationStaff
                    .Where(s => s.RevokedAt != null)
                    .Select(s => $"revoke:{s.Id}:{s.RevokedAt!.Value.Ticks}")
                    .ToListAsync(ct);
                ids.AddRange(revokeIds);
            }
            else if (isStaff)
            {
                var revokeMine = await _db.StationStaff
                    .Where(s => s.UserId == userId && s.RevokedAt != null)
                    .Select(s => $"revoke:{s.Id}:{s.RevokedAt!.Value.Ticks}")
                    .ToListAsync(ct);
                ids.AddRange(revokeMine);
            }

            return ids.Distinct().ToList();
        }

        public async Task<bool> IsVisibleIdAsync(int userId, string role, string notifId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(notifId)) return false;

            var firstColon = notifId.IndexOf(':');
            if (firstColon <= 0) return false;
            var prefix = notifId[..firstColon];
            var rest = notifId[(firstColon + 1)..];

            var secondColon = rest.IndexOf(':');
            var idPart = secondColon >= 0 ? rest[..secondColon] : rest;
            if (!int.TryParse(idPart, out var entityId)) return false;

            bool isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            bool isStaff = role.Equals("Staff", StringComparison.OrdinalIgnoreCase);
            bool isDriver = role.Equals("Driver", StringComparison.OrdinalIgnoreCase);

            switch (prefix)
            {
                case "booking":
                    if (isAdmin) return await _db.Bookings.AnyAsync(b => b.Id == entityId, ct);
                    // staff/driver: booking của chính mình
                    return await _db.Bookings.AnyAsync(b => b.Id == entityId && b.UserId == userId, ct);

                case "tx":
                    if (isAdmin) return await _db.Transactions.AnyAsync(t => t.Id == entityId, ct);
                    // staff/driver: transaction thuộc ví của mình
                    return await _db.Transactions.Include(t => t.Wallet)
                        .AnyAsync(t => t.Id == entityId && t.Wallet.UserId == userId, ct);

                case "deposit":
                    if (isAdmin) return await _db.Transactions.AnyAsync(t => t.Id == entityId, ct);
                    if (isStaff)
                        return await _db.Transactions.AnyAsync(
                            t => t.Id == entityId && t.Note != null &&
                                 EF.Functions.Like(t.Note, $"%STAFF_DEPOSIT:{userId}%"), ct);
                    return false;

                case "incident":
                    if (isAdmin) return await _db.Incidents.AnyAsync(i => i.Id == entityId, ct);
                    if (isStaff)
                    {
                        var stationIds = await _db.StationStaff
                            .Where(s => s.UserId == userId && s.RevokedAt == null)
                            .Select(s => s.StationId).ToListAsync(ct);
                        return await _db.Incidents.AnyAsync(i => i.Id == entityId && stationIds.Contains(i.StationId), ct);
                    }
                    return false;

                case "assign":
                    return isAdmin && await _db.StationStaff.AnyAsync(ss => ss.Id == entityId, ct);

                case "revoke":
                    if (isAdmin) return await _db.StationStaff.AnyAsync(ss => ss.Id == entityId, ct);
                    if (isStaff) return await _db.StationStaff.AnyAsync(ss => ss.Id == entityId && ss.UserId == userId, ct);
                    return false;
            }

            return false;
        }
    }
}
