using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Dto.VehicleModels;
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
    public class VehicleModelRepository(AppDbContext db) : IVehicleModelRepository
    {
        public Task<List<VehicleModel>> ListAsync(string? search, CancellationToken ct)
           => db.VehicleModels.AsNoTracking()
               .Include(x => x.VehicleModelConnectorTypes)
               .Where(x => string.IsNullOrWhiteSpace(search) || x.ModelName.Contains(search))
               .OrderBy(x => x.ModelName)
               .ToListAsync(ct);

        public Task<VehicleModel?> GetByIdAsync(int id, CancellationToken ct)
            => db.VehicleModels.FirstOrDefaultAsync(x => x.Id == id, ct);

        public Task<VehicleModel?> GetDetailAsync(int id, CancellationToken ct)
            => db.VehicleModels
                .Include(vm => vm.VehicleModelConnectorTypes)
                .ThenInclude(j => j.ConnectorType)
                .FirstOrDefaultAsync(x => x.Id == id, ct);

        public Task<bool> ExistsByNameAsync(string modelName, int? excludeId, CancellationToken ct)
            => db.VehicleModels
                .Where(x => !excludeId.HasValue || x.Id != excludeId.Value)
                .AnyAsync(x => x.ModelName == modelName, ct);

        public Task AddAsync(VehicleModel entity, CancellationToken ct)
            => db.VehicleModels.AddAsync(entity, ct).AsTask();

        public void Remove(VehicleModel entity) => db.VehicleModels.Remove(entity);

        public async Task<bool> AllConnectorTypesExistAsync(IEnumerable<int> ids, CancellationToken ct)
        {
            var set = ids?.Distinct().ToList() ?? new List<int>();
            if (set.Count == 0) return true;
            var count = await db.ConnectorTypes.Where(x => set.Contains(x.Id)).CountAsync(ct);
            return count == set.Count;
        }

        public async Task RemoveAllJoinsAsync(int vehicleModelId, CancellationToken ct)
        {
            var joins = await db.Set<VehicleModelConnectorType>()
                                .Where(j => j.VehicleModelId == vehicleModelId)
                                .ToListAsync(ct);
            db.RemoveRange(joins);
        }

        public Task AddJoinsAsync(int vehicleModelId, IEnumerable<int> connectorTypeIds, CancellationToken ct)
        {
            var joins = connectorTypeIds.Distinct().Select(id => new VehicleModelConnectorType
            {
                VehicleModelId = vehicleModelId,
                ConnectorTypeId = id
            });
            db.Set<VehicleModelConnectorType>().AddRange(joins);
            return Task.CompletedTask;
        }

        public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
