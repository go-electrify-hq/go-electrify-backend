using System.Data;
using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GoElectrify.DAL.Repositories
{
    public sealed class ChargingSessionRepository(AppDbContext db) : IChargingSessionRepository
    {
        public Task<Charger?> GetChargerAsync(int chargerId, CancellationToken ct)
            => db.Chargers.FirstOrDefaultAsync(c => c.Id == chargerId, ct);

        public Task<int> CountActiveOnChargerAsync(int chargerId, CancellationToken ct)
            => db.ChargingSessions.CountAsync(s => s.ChargerId == chargerId && s.EndedAt == null, ct);

        public Task<bool> VehicleSupportsConnectorAsync(int vehicleModelId, int connectorTypeId, CancellationToken ct)
            => db.Set<VehicleModelConnectorType>()
                 .AnyAsync(x => x.VehicleModelId == vehicleModelId && x.ConnectorTypeId == connectorTypeId, ct);

        public Task<Booking?> FindBookingByCodeForUserAsync(string code, int userId, CancellationToken ct)
            => db.Bookings.FirstOrDefaultAsync(b => b.Code == code && b.UserId == userId, ct);

        public async Task AddSessionAsync(ChargingSession session, CancellationToken ct)
            => await db.ChargingSessions.AddAsync(session, ct);

        public Task<ChargingSession?> GetSessionAsync(int id, CancellationToken ct)
            => db.ChargingSessions.FirstOrDefaultAsync(s => s.Id == id, ct);

        public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

        public async Task<IDbContextTransaction> BeginSerializableTxAsync(CancellationToken ct) => await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        public Task<Charger?> FindAvailableChargerAsync(int stationId, int connectorTypeId, CancellationToken ct)
        => db.Chargers
            .AsNoTracking()
            .Where(c => c.StationId == stationId
                     && c.ConnectorTypeId == connectorTypeId
                     && c.Status == "ONLINE")
            .Where(c => !db.ChargingSessions.Any(s => s.ChargerId == c.Id && s.EndedAt == null))
            .OrderByDescending(c => c.PowerKw)
            .ThenBy(c => c.Id)
            .FirstOrDefaultAsync(ct);

        public Task<VehicleModel?> GetVehicleModelAsync(int id, CancellationToken ct)
        => db.VehicleModels.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        public Task<ConnectorType?> GetConnectorTypeAsync(int id, CancellationToken ct)
        => db.ConnectorTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

        public async Task<List<ChargingSession>> GetByStationAsync(
        int stationId,
        string? status,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct)
        {
            var size = (pageSize <= 0 || pageSize > 200) ? 20 : pageSize;
            var skip = Math.Max(0, (page <= 0 ? 1 : page) - 1) * size;

            var q = db.ChargingSessions.AsNoTracking()
                // filter theo Station từ CHARGER như yêu cầu của bạn
                .Where(s => s.Charger.StationId == stationId);

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(s => s.Status == status);

            if (from.HasValue)
                q = q.Where(s => s.StartedAt >= from.Value);

            if (to.HasValue)
                q = q.Where(s => s.StartedAt < to.Value);

            // Include đủ các navigation để map sang DTO
            q = q.Include(s => s.Charger)
                 .ThenInclude(c => c.ConnectorType)
                .Include(s => s.Booking.VehicleModel);

            return await q.OrderByDescending(s => s.StartedAt)
                          .Skip(skip)
                          .Take(size)
                          .ToListAsync(ct);
        }
    }
}
