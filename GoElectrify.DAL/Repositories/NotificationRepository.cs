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

    public async Task<List<NotificationDto>> GetByRoleAsync(int userId, string role, CancellationToken ct)
    {
        DateTime since = DateTime.UtcNow.AddDays(-7);
        var bag = new List<NotificationDto>();

        bool isDriver = role.Equals("Driver", StringComparison.OrdinalIgnoreCase);
        bool isStaff  = role.Equals("Staff",  StringComparison.OrdinalIgnoreCase);
        bool isAdmin  = role.Equals("Admin",  StringComparison.OrdinalIgnoreCase);

        // === DRIVER: Booking, Wallet, Subscription, Charging, Transaction ===
        if (isDriver)
        {
            // Booking (mọi trạng thái)
            var bookings = await _db.Bookings.Include(b => b.Station)
                .Where(b => b.UserId == userId && (b.CreatedAt >= since || b.UpdatedAt >= since))
                .ToListAsync(ct);

            foreach (var b in bookings)
            {
                string type = $"booking.{b.Status.ToLower()}";
                string title = b.Status switch
                {
                    "CONFIRMED" => "Đặt chỗ xác nhận thành công",
                    "FAILED"    => "Đặt chỗ thất bại",
                    "CANCELED"  => "Đặt chỗ đã huỷ",
                    "EXPIRED"   => "Đặt chỗ hết hạn",
                    _           => "Đặt chỗ mới"
                };
                bag.Add(new NotificationDto
                {
                    Id = $"booking:{b.Id}",
                    Type = type,
                    Title = title,
                    Message = $"Trạm {b.Station.Name}",
                    Severity = b.Status == "FAILED" ? "HIGH" : "LOW",
                    CreatedAt = b.UpdatedAt
                });
            }

            // Transaction (wallet/payment/subscription)
            var txs = await _db.Transactions.Include(t => t.Wallet)
                .Where(t => t.Wallet.UserId == userId && t.CreatedAt >= since)
                .ToListAsync(ct);

            foreach (var t in txs)
            {
                string type = $"{t.Type.ToLower()}.{t.Status.ToLower()}";
                string title = t.Status switch
                {
                    "SUCCESS"  => $"{t.Type} thành công",
                    "FAILED"   => $"{t.Type} thất bại",
                    "REFUNDED" => $"{t.Type} hoàn tiền",
                    _          => $"{t.Type} cập nhật"
                };

                string severity = t.Status switch
                {
                    "FAILED"   => "HIGH",
                    "REFUNDED" => "MEDIUM",
                    _          => "LOW"
                };

                bag.Add(new NotificationDto
                {
                    Id = $"tx:{t.Id}",
                    Type = $"transaction.{type}",
                    Title = title,
                    Message = $"{t.Type} • {t.Amount:n0}đ",
                    Severity = severity,
                    CreatedAt = t.CreatedAt
                });
            }

            // ChargingSession
            /*
            var cs = await _db.ChargingSessions.Include(c => c.)
                .Where(c => c.Id == userId && (c.CreatedAt >= since || (c.EndedAt != null && c.EndedAt >= since)))
                .ToListAsync(ct);

            foreach (var s in cs)
            {
                if (s.Status == "ERROR")
                {
                    bag.Add(new NotificationDto
                    {
                        Id = $"charge:{s.Id}:error",
                        Type = "charging.error",
                        Title = "Phiên sạc gặp lỗi",
                        Message = $"Trạm {s.Station.Name} • {s.StopReason}",
                        Severity = "HIGH",
                        CreatedAt = s.EndedAt ?? s.CreatedAt
                    });
                }
                else
                {
                    bag.Add(new NotificationDto
                    {
                        Id = $"charge:{s.Id}:{s.Status.ToLower()}",
                        Type = $"charging.{s.Status.ToLower()}",
                        Title = s.Status == "STARTED" ? "Bắt đầu sạc" : "Kết thúc sạc",
                        Message = $"Trạm {s.Station.Name}",
                        Severity = "LOW",
                        CreatedAt = s.EndedAt ?? s.CreatedAt
                    });
                }
            }*/
        }

        // === STAFF: Incident trạm mình, Remote start/stop, Deposit to customer ===
        if (isStaff)
        {
            var stations = await _db.StationStaff
                .Where(s => s.UserId == userId)
                .Select(s => s.StationId)
                .ToListAsync(ct);

            // Incident trạm mình
            var incidents = await _db.Incidents.Include(i => i.Station)
                .Where(i => stations.Contains(i.StationId) && i.CreatedAt >= since)
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

            // Remote actions (do staff thực hiện)
            /*
            var remote = await _db.ChargingSessions.Include(s => s.Station)
                .Where(s =>
                    (s.RemoteStartedByStaffId == userId && s.CreatedAt >= since) ||
                    (s.RemoteStoppedByStaffId == userId && s.EndedAt >= since))
                .ToListAsync(ct);

            foreach (var s in remote)
            {
                string type = s.RemoteStoppedByStaffId == userId
                    ? "charging.remote.stop"
                    : "charging.remote.start";
                string title = type.EndsWith("stop")
                    ? "Bạn đã Remote Stop một phiên sạc"
                    : "Bạn đã Remote Start một phiên sạc";
                bag.Add(new NotificationDto
                {
                    Id = $"staff:{userId}:{s.Id}:{type}",
                    Type = type,
                    Title = title,
                    Message = $"Trạm {s.Station.Name}",
                    Severity = "LOW",
                    CreatedAt = s.EndedAt ?? s.CreatedAt
                });
            }*/

            // Deposit to customer
            var deposit = await _db.Transactions
                .Where(t => t.Note != null && t.Note.Contains($"STAFF_DEPOSIT:{userId}")
                            && t.CreatedAt >= since)
                .ToListAsync(ct);

            foreach (var t in deposit)
            {
                string type = $"wallet.staffdeposit.{t.Status.ToLower()}";
                string title = t.Status == "SUCCESS" ? "Nạp hộ khách thành công" : "Nạp hộ khách thất bại";
                string sev = t.Status == "FAILED" ? "HIGH" : "LOW";
                bag.Add(new NotificationDto
                {
                    Id = $"deposit:{t.Id}",
                    Type = type,
                    Title = title,
                    Message = $"{t.Amount:n0}đ",
                    Severity = sev,
                    CreatedAt = t.CreatedAt
                });
            }
        }

        // === ADMIN: Toàn hệ thống ===
        if (isAdmin)
        {
            var incidents = await _db.Incidents.Include(i => i.Station)
                .Where(i => i.CreatedAt >= since)
                .Select(i => new NotificationDto
                {
                    Id = $"incident:{i.Id}",
                    Type = "incident.reported",
                    Title = "Sự cố mới toàn hệ thống",
                    Message = $"Trạm {i.Station.Name}",
                    Severity = i.Priority ?? "MEDIUM",
                    CreatedAt = i.CreatedAt
                }).ToListAsync(ct);
            bag.AddRange(incidents);

            var assigns = await _db.StationStaff.Include(s => s.Station)
                .Where(s => s.AssignedAt >= since)
                .Select(s => new NotificationDto
                {
                    Id = $"assign:{s.Id}",
                    Type = "station.staff.assigned",
                    Title = "Gán nhân viên vào trạm",
                    Message = $"Trạm {s.Station.Name}",
                    Severity = "LOW",
                    CreatedAt = s.AssignedAt
                }).ToListAsync(ct);
            bag.AddRange(assigns);
        }

        return bag.OrderByDescending(x => x.CreatedAt).ToList();
    }

    // Dùng khi mark-all-read
    public async Task<List<string>> GetAllIdsAsync(int userId, CancellationToken ct)
    {
        var ids = await _db.Bookings
            .Where(b => b.UserId == userId)
            .Select(b => $"booking:{b.Id}")
            .ToListAsync(ct);

        var tx = await _db.Transactions.Include(t => t.Wallet)
            .Where(t => t.Wallet.UserId == userId)
            .Select(t => $"tx:{t.Id}")
            .ToListAsync(ct);

        ids.AddRange(tx);
        return ids;
    }


        public async Task<bool> IsVisibleIdAsync(int userId, string role, string notifId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(notifId)) return false;

            // prefix:id[:...]
            var firstColon = notifId.IndexOf(':');
            if (firstColon <= 0) return false;
            var prefix = notifId[..firstColon];
            var rest = notifId[(firstColon + 1)..];

            // lấy số id đầu (trước dấu ':' tiếp theo, nếu có)
            var secondColon = rest.IndexOf(':');
            var idPart = secondColon >= 0 ? rest[..secondColon] : rest;
            if (!int.TryParse(idPart, out var entityId)) return false;

            bool isAdmin = role.Equals("Admin", StringComparison.OrdinalIgnoreCase);
            bool isStaff = role.Equals("Staff", StringComparison.OrdinalIgnoreCase);
            bool isDriver = role.Equals("Driver", StringComparison.OrdinalIgnoreCase);

            switch (prefix)
            {
                case "booking":
                    {
                        if (isAdmin) return await _db.Bookings.AnyAsync(b => b.Id == entityId, ct);
                        if (isDriver) return await _db.Bookings.AnyAsync(b => b.Id == entityId && b.UserId == userId, ct);
                        if (isStaff)
                        {
                            var stationIds = await _db.StationStaff
                                .Where(s => s.UserId == userId && s.RevokedAt == null)
                                .Select(s => s.StationId).ToListAsync(ct);
                            return await _db.Bookings.AnyAsync(b => b.Id == entityId && stationIds.Contains(b.StationId), ct);
                        }
                        return false;
                    }

                case "tx": // transaction
                case "deposit": // staff deposit id dạng deposit:{id}
                    {
                        if (isAdmin) return await _db.Transactions.AnyAsync(t => t.Id == entityId, ct);
                        if (isDriver)
                            return await _db.Transactions.Include(t => t.Wallet)
                                .AnyAsync(t => t.Id == entityId && t.Wallet.UserId == userId, ct);
                        if (isStaff)
                            return await _db.Transactions
                                .AnyAsync(t => t.Id == entityId && t.Note != null &&
                                               EF.Functions.Like(t.Note, $"%STAFF_DEPOSIT:{userId}%"), ct);
                    }
                    break;

                case "incident":
                    {
                        if (isAdmin) return await _db.Incidents.AnyAsync(i => i.Id == entityId, ct);
                        if (isStaff)
                        {
                            var stationIds = await _db.StationStaff
                                .Where(s => s.UserId == userId && s.RevokedAt == null)
                                .Select(s => s.StationId).ToListAsync(ct);
                            return await _db.Incidents.AnyAsync(i => i.Id == entityId && stationIds.Contains(i.StationId), ct);
                        }
                        return false;
                    }

                case "assign":
                case "station": // station:{stationId}:staff:{userId}:assigned
                    {
                        if (!isAdmin) return false;
                        return await _db.StationStaff.AnyAsync(ss => ss.Id == entityId, ct);
                    }
            }

            return false;
        }
    }
}
