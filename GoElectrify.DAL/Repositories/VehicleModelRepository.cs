using GoElectrify.BLL.Contracts.Repositories;
using GoElectrify.BLL.Entities;
using GoElectrify.DAL.Persistence;
using Microsoft.EntityFrameworkCore;

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

        public Task<List<int>> FindIdsInBookingsAsync(IEnumerable<int> ids, CancellationToken ct)
        {
            var idList = ids?.Where(x => x > 0).Distinct().ToList() ?? new();
            if (idList.Count == 0) return Task.FromResult(new List<int>());

            return db.Set<Booking>()
                     .Where(b => b.VehicleModelId.HasValue
                              && idList.Contains(b.VehicleModelId.Value))
                     .Select(b => b.VehicleModelId!.Value)
                     .Distinct()
                     .ToListAsync(ct);
        }

        // Dọn join cho nhiều VM (set-based, nhanh)
        public Task RemoveAllJoinsForManyAsync(IEnumerable<int> ids, CancellationToken ct)
        {
            var idList = ids?.Distinct().ToList() ?? new();
            if (idList.Count == 0) return Task.CompletedTask;

            return db.VehicleModelConnectorTypes
                     .Where(j => idList.Contains(j.VehicleModelId))
                     .ExecuteDeleteAsync(ct);
        }

        // Xoá nhiều VM (set-based)
        public Task<int> DeleteManyAsync(IEnumerable<int> ids, CancellationToken ct)
        {
            var idList = ids?.Distinct().ToList() ?? new();
            if (idList.Count == 0) return Task.FromResult(0);

            return db.VehicleModels
                     .Where(x => idList.Contains(x.Id))
                     .ExecuteDeleteAsync(ct);
        }

        // Gộp 2 lệnh DELETE (joins + parent) vào 1 transaction cho an toàn
        public async Task<int> DeleteManySafeAsync(IEnumerable<int> ids, CancellationToken ct)
        {
            var idList = ids?.Distinct().ToList() ?? new();
            if (idList.Count == 0) return 0;

            var strategy = db.Database.CreateExecutionStrategy(); // cần khi EnableRetryOnFailure
            int affected = 0;

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync(ct);
                try
                {
                    // 1) Dọn join (set-based). Nếu lỗi -> catch -> Rollback toàn bộ.
                    await db.Set<VehicleModelConnectorType>()
                            .Where(j => idList.Contains(j.VehicleModelId))
                            .ExecuteDeleteAsync(ct);

                    // 2) Xóa VehicleModels (set-based). Nếu lỗi -> catch -> Rollback toàn bộ.
                    affected = await db.VehicleModels
                                       .Where(x => idList.Contains(x.Id))
                                       .ExecuteDeleteAsync(ct);

                    await tx.CommitAsync(ct); // chỉ commit khi cả 2 lệnh đều OK
                }
                catch
                {
                    await tx.RollbackAsync(ct); // rollback mọi thay đổi trong transaction
                    throw; // ném lỗi ra ngoài cho service/controller xử lý
                }
            });

            return affected;
        }
        public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
    }
}
