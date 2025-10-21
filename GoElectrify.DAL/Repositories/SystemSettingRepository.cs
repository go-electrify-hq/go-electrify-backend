using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public class SystemSettingRepository : ISystemSettingRepository
    {
        private readonly AppDbContext _db;
        public SystemSettingRepository(AppDbContext db) => _db = db;

        public async Task<string?> GetAsync(string key, CancellationToken ct)
        {
            return await _db.SystemSettings
                .AsNoTracking()
                .Where(x => x.Key == key)
                .Select(x => x.Value)
                .FirstOrDefaultAsync(ct);
        }

        public async Task UpsertAsync(string key, string value, int? updatedBy, CancellationToken ct)
        {
            var e = await _db.SystemSettings.FirstOrDefaultAsync(x => x.Key == key, ct);
            if (e == null)
            {
                _db.SystemSettings.Add(new()
                {
                    Key = key,
                    Value = value,
                    UpdatedBy = updatedBy,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                e.Value = value;
                e.UpdatedBy = updatedBy;
                e.UpdatedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync(ct);
        }
    }
}
