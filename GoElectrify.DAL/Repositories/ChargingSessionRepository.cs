using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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


    }
}
