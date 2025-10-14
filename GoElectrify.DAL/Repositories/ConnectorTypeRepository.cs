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
    public class ConnectorTypeRepository(AppDbContext db) : IConnectorTypeRepository
    {
        public Task<List<ConnectorType>> ListAsync(string? search, CancellationToken ct)
        {
            var q = db.ConnectorTypes.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToUpper();

                q = q.Where(x =>
                    x.Name.ToUpper().Contains(s) ||                                  // tìm theo Name
                    (x.Description != null && x.Description.ToUpper().Contains(s)) // tìm theo Description 
                );
            }

            return q.OrderBy(x => x.Name).ToListAsync(ct);
        }   

        public Task<ConnectorType?> GetByIdAsync(int id, CancellationToken ct)
            => db.ConnectorTypes.FirstOrDefaultAsync(x => x.Id == id, ct);

        public Task<bool> ExistsByNameAsync(string name, int? excludeId, CancellationToken ct)
        //=> db.ConnectorTypes
        //     .Where(x => !excludeId.HasValue || x.Id != excludeId.Value)
        //     .AnyAsync(x => x.Name == name, ct);
        {
            var n = name.Trim().ToUpper();
            return db.ConnectorTypes
                     .Where(x => !excludeId.HasValue || x.Id != excludeId.Value)
                     .AnyAsync(x => x.Name.ToUpper() == n, ct);
        }

        public Task AddAsync(ConnectorType entity, CancellationToken ct)
            => db.ConnectorTypes.AddAsync(entity, ct).AsTask();

        public void Remove(ConnectorType entity) => db.ConnectorTypes.Remove(entity);

        public async Task<HashSet<int>> GetExistingIdsAsync(IEnumerable<int> ids, CancellationToken ct)
        {
            var list = ids.Distinct().ToList();
            if (list.Count == 0) return new();

            var existing = await db.ConnectorTypes.AsNoTracking()
                                .Where(x => list.Contains(x.Id))
                                .Select(x => x.Id)
                                .ToListAsync(ct);
            return existing.ToHashSet();
        }

        // CHỈ coi là blocked khi bị tham chiếu bởi Chargers hoặc Bookings
        public async Task<HashSet<int>> FindBlockedIdsAsync(IEnumerable<int> ids, CancellationToken ct)
        {
            var list = ids.Distinct().ToList();
            if (list.Count == 0) return new();

            var inChargers = await db.Chargers.AsNoTracking()
                                .Where(c => list.Contains(c.ConnectorTypeId))
                                .Select(c => c.ConnectorTypeId)
                                .Distinct()
                                .ToListAsync(ct);

            var inBookings = await db.Bookings.AsNoTracking()
                                .Where(b => list.Contains(b.ConnectorTypeId))
                                .Select(b => b.ConnectorTypeId)
                                .Distinct()
                                .ToListAsync(ct);

            return inChargers.Concat(inBookings).ToHashSet();
        }

        // Xoá join VehicleModelConnectorTypes cho 1 id
        public Task RemoveAllJoinsAsync(int connectorTypeId, CancellationToken ct)
            => db.Set<VehicleModelConnectorType>()
                 .Where(j => j.ConnectorTypeId == connectorTypeId)
                 .ExecuteDeleteAsync(ct);

        // Xoá join VehicleModelConnectorTypes cho nhiều id
        public Task RemoveAllJoinsAsync(IEnumerable<int> ids, CancellationToken ct)
        {
            var list = ids.Distinct().ToList();
            if (list.Count == 0) return Task.CompletedTask;

            return db.Set<VehicleModelConnectorType>()
                     .Where(j => list.Contains(j.ConnectorTypeId))
                     .ExecuteDeleteAsync(ct);
        }

        // Xoá parent set-based
        public Task<int> BulkDeleteAsync(IEnumerable<int> ids, CancellationToken ct)
        {
            var list = ids.Distinct().ToList();
            if (list.Count == 0) return Task.FromResult(0);

            return db.ConnectorTypes
                     .Where(x => list.Contains(x.Id))
                     .ExecuteDeleteAsync(ct);
        }

        // Dọn join + xoá parent trong 1 transaction (có retry) — all-or-nothing
        public async Task<int> DeleteManySafeAsync(IEnumerable<int> ids, CancellationToken ct)
        {
            var list = ids.Distinct().ToList();
            if (list.Count == 0) return 0;

            var strategy = db.Database.CreateExecutionStrategy();
            int affected = 0;

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(ct);
                try
                {
                    await db.Set<VehicleModelConnectorType>()
                            .Where(j => list.Contains(j.ConnectorTypeId))
                            .ExecuteDeleteAsync(ct);

                    affected = await db.ConnectorTypes
                                       .Where(x => list.Contains(x.Id))
                                       .ExecuteDeleteAsync(ct);

                    await tx.CommitAsync(ct);
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }
            });

            return affected;
        }

        public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
