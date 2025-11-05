using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GoElectrify.DAL.Repositories
{
    public sealed class ChargerLogRepository(AppDbContext db) : IChargerLogRepository
    {
        public async Task<List<ChargerLog>> GetLastByChargerBetweenAsync(int chargerId, DateTime fromUtc, DateTime toUtc, int last, CancellationToken ct)
        {
            last = Math.Clamp(last <= 0 ? 200 : last, 50, 2000);
            var q = db.ChargerLogs
                .AsNoTracking()
                .Where(l => l.ChargerId == chargerId && l.SampleAt >= fromUtc && l.SampleAt <= toUtc)
                .OrderByDescending(l => l.SampleAt)
                .Take(last)
                .OrderBy(l => l.SampleAt);

            return await q.ToListAsync(ct);
        }
    }
}
