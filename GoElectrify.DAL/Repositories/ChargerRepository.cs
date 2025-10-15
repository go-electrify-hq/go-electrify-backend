using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public sealed class ChargerRepository : IChargerRepository
    {
        private readonly AppDbContext _db;
        public ChargerRepository(AppDbContext db) => _db = db;

        public Task<List<Charger>> GetAllAsync(CancellationToken ct) =>
            _db.Chargers.AsNoTracking()
                .OrderBy(c => c.StationId).ThenBy(c => c.Code)
                .ToListAsync(ct);

        public Task<Charger?> GetByIdAsync(int id, CancellationToken ct) =>
            _db.Chargers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

        public async Task AddAsync(Charger entity, CancellationToken ct)
        {
            _db.Chargers.Add(entity);
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Charger entity, CancellationToken ct)
        {
            _db.Chargers.Update(entity);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken ct)
        {
            var e = await _db.Chargers.FindAsync(new object[] { id }, ct);
            if (e is null) return false;
            _db.Chargers.Remove(e);
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public Task<bool> CodeExistsAsync(int stationId, string code, int? exceptId, CancellationToken ct) =>
            _db.Chargers.AnyAsync(c =>
                c.StationId == stationId &&
                c.Code == code &&
                (exceptId == null || c.Id != exceptId.Value), ct);

        public Task<List<Charger>> GetByStationAsync(int stationId, CancellationToken ct) =>
            _db.Chargers.AsNoTracking()
                .Where(c => c.StationId == stationId)
                .OrderBy(c => c.Code)
                .ToListAsync(ct);
    }
}
