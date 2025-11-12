using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public sealed class BookingRepository(AppDbContext db) : IBookingRepository
    {
        public Task<Booking?> GetByIdAsync(int id, CancellationToken ct)
            => db.Bookings.FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task AddAsync(Booking e, CancellationToken ct)
        {
            await db.Bookings.AddAsync(e, ct);
            await db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Booking e, CancellationToken ct)
        {
            db.Bookings.Update(e);
            await db.SaveChangesAsync(ct);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct)
        {
            var e = await db.Bookings.FindAsync(new object[] { id }, ct);
            if (e is null) return false;
            db.Bookings.Remove(e);
            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IReadOnlyList<Booking>> GetMyAsync(
            int userId, string? status, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
        {
            var q = db.Bookings.AsNoTracking().Where(b => b.UserId == userId);
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(b => b.Status == status);
            if (from.HasValue) q = q.Where(b => b.ScheduledStart >= from.Value);
            if (to.HasValue) q = q.Where(b => b.ScheduledStart < to.Value);

            return await q
                .OrderByDescending(b => b.ScheduledStart)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
        }

        public Task<int> CountActiveBookingsAsync(
            int stationId, int connectorTypeId, DateTime windowStartUtc, DateTime windowEndUtc, CancellationToken ct)
        {
            var active = new[] { "PENDING", "CONFIRMED", "CONSUMED" };
            return db.Bookings
                .Where(b => b.StationId == stationId && b.ConnectorTypeId == connectorTypeId)
                .Where(b => active.Contains(b.Status))
                .Where(b => b.ScheduledStart >= windowStartUtc && b.ScheduledStart < windowEndUtc)
                .CountAsync(ct);
        }

        public Task<int> CountActiveChargersAsync(int stationId, int connectorTypeId, CancellationToken ct)
            => db.Chargers
                  .Where(c => c.StationId == stationId && c.ConnectorTypeId == connectorTypeId)
                  .Where(c => c.Status == "ONLINE")
                  .CountAsync(ct);

        public Task<bool> VehicleSupportsConnectorAsync(int vehicleModelId, int connectorTypeId, CancellationToken ct)
            => db.Set<VehicleModelConnectorType>()
                 .AnyAsync(j => j.VehicleModelId == vehicleModelId && j.ConnectorTypeId == connectorTypeId, ct);

        public Task<bool> StationExistsAsync(int stationId, CancellationToken ct)
            => db.Stations.AnyAsync(s => s.Id == stationId, ct);
        public async Task<List<Booking>> GetByStationAsync(
        int stationId,
        string? status,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct)
        {
            var q = db.Bookings.AsNoTracking()
                .Where(b => b.StationId == stationId);

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(b => b.Status == status);

            if (from.HasValue)
                q = q.Where(b => b.ScheduledStart >= from.Value);

            if (to.HasValue)
                q = q.Where(b => b.ScheduledStart < to.Value);

            var skip = (page <= 0 ? 0 : (page - 1) * pageSize);

            return await q.OrderByDescending(b => b.ScheduledStart)
                          .Skip(skip)
                          .Take(pageSize)
                          .ToListAsync(ct);
        }

        public async Task<string?> GetUserEmailAsync(int userId, CancellationToken ct)
        {
            return await db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Booking?> GetByCodeAsync(string code, CancellationToken ct)
        {
            return await db.Bookings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Code == code, ct);
        }
    }
}
