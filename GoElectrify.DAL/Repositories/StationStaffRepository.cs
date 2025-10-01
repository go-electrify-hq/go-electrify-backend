using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.DAL.Repositories
{
    public class StationStaffRepository(AppDbContext db) : IStationStaffRepository
    {
        public Task<StationStaff?> GetAsync(int stationId, int userId, CancellationToken ct)
           => db.StationStaff
                .Include(x => x.User)
                .Include(x => x.Station)
                .FirstOrDefaultAsync(x => x.StationId == stationId && x.UserId == userId, ct);

        public Task<List<StationStaff>> ListByStationAsync(int stationId, CancellationToken ct)
            => db.StationStaff
                 .Include(x => x.User)
                 .Where(x => x.StationId == stationId)   // không lọc revoke vì ta không dùng revoke
                 .OrderBy(x => x.Role).ThenBy(x => x.UserId)
                 .ToListAsync(ct);

        public Task AddAsync(StationStaff entity, CancellationToken ct)
            => db.StationStaff.AddAsync(entity, ct).AsTask();

        public void Update(StationStaff entity) => db.StationStaff.Update(entity);

        public void Remove(StationStaff entity) => db.StationStaff.Remove(entity);

        public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
