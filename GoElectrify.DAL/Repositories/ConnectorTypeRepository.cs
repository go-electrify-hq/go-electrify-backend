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
           => db.ConnectorTypes
                .AsNoTracking()
                .Where(x => string.IsNullOrWhiteSpace(search) || x.Name.Contains(search))
                .OrderBy(x => x.Name)
                .ToListAsync(ct);

        public Task<ConnectorType?> GetByIdAsync(int id, CancellationToken ct)
            => db.ConnectorTypes.FirstOrDefaultAsync(x => x.Id == id, ct);

        public Task<bool> ExistsByNameAsync(string name, int? excludeId, CancellationToken ct)
            => db.ConnectorTypes
                 .Where(x => !excludeId.HasValue || x.Id != excludeId.Value)
                 .AnyAsync(x => x.Name == name, ct);

        public Task AddAsync(ConnectorType entity, CancellationToken ct)
            => db.ConnectorTypes.AddAsync(entity, ct).AsTask();

        public void Remove(ConnectorType entity) => db.ConnectorTypes.Remove(entity);

        public Task<bool> HasAnyJoinAsync(int connectorTypeId, CancellationToken ct)
            => db.Set<VehicleModelConnectorType>()
                 .AnyAsync(j => j.ConnectorTypeId == connectorTypeId, ct);

        public async Task RemoveAllJoinsAsync(int connectorTypeId, CancellationToken ct)
        {
            var joins = await db.Set<VehicleModelConnectorType>()
                                .Where(j => j.ConnectorTypeId == connectorTypeId)
                                .ToListAsync(ct);
            db.RemoveRange(joins);
        }

        public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
