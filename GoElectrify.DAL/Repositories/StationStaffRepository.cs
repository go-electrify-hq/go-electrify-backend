using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public class StationStaffRepository : IStationStaffRepository
    {
        private readonly AppDbContext _db;
        public StationStaffRepository(AppDbContext db) => _db = db;

        public Task<StationStaff?> GetAsync(int stationId, int userId, CancellationToken ct)
            => _db.StationStaff
                 .Include(x => x.User)
                 .Include(x => x.Station)
                 .FirstOrDefaultAsync(x => x.StationId == stationId && x.UserId == userId, ct);

        public Task<StationStaff?> GetActiveByUserAsync(int userId, CancellationToken ct) // NEW
            => _db.StationStaff
                 .Include(x => x.Station)
                 .FirstOrDefaultAsync(x => x.UserId == userId && x.RevokedAt == null, ct);

        public async Task<List<StationStaff>> ListByStationAsync(int stationId, bool includeRevoked, CancellationToken ct)
        {
            var q = _db.StationStaff
                       .Include(x => x.User)
                       .Where(x => x.StationId == stationId);

            if (!includeRevoked) q = q.Where(x => x.RevokedAt == null);

            return await q.OrderBy(x => x.UserId).ToListAsync(ct);
        }

        public Task AddAsync(StationStaff entity, CancellationToken ct)
            => _db.StationStaff.AddAsync(entity, ct).AsTask();

        public void Update(StationStaff entity) => _db.StationStaff.Update(entity);

        public Task SaveAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}
