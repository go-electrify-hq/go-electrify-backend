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

        public async Task<(int Total, List<ChargerLog> Items)> GetLogsPagedAsync(
            int chargerId,
            DateTime? from, DateTime? to,
            string[] states, string[] errorCodes,
            bool asc,
            int page, int pageSize,
            CancellationToken ct)
        {
            var q = db.ChargerLogs.AsNoTracking()
                .Where(l => l.ChargerId == chargerId);

            if (from.HasValue) q = q.Where(l => l.SampleAt >= from.Value);
            if (to.HasValue) q = q.Where(l => l.SampleAt <= to.Value);

            if (states is { Length: > 0 })
            {
                // so sánh case-insensitive: UPPER ở cả hai phía
                q = q.Where(l => l.State != null && states.Contains(l.State.ToUpper()));
            }

            if (errorCodes is { Length: > 0 })
            {
                q = q.Where(l => l.ErrorCode != null && errorCodes.Contains(l.ErrorCode.ToUpper()));
            }

            q = asc
                ? q.OrderBy(l => l.SampleAt).ThenBy(l => l.Id)
                : q.OrderByDescending(l => l.SampleAt).ThenByDescending(l => l.Id);

            var total = await q.CountAsync(ct);
            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (total, items);
        }
    }
}