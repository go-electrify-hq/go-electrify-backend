using GoElectrify.BLL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IVehicleModelRepository
    {
        Task<List<VehicleModel>> ListAsync(string? search, CancellationToken ct);
        Task<VehicleModel?> GetByIdAsync(int id, CancellationToken ct);
        Task<VehicleModel?> GetDetailAsync(int id, CancellationToken ct);
        Task<bool> ExistsByNameAsync(string modelName, int? excludeId, CancellationToken ct);

        Task<bool> AllConnectorTypesExistAsync(IEnumerable<int> ids, CancellationToken ct);

        Task AddAsync(VehicleModel entity, CancellationToken ct);
        void Remove(VehicleModel entity);

        Task RemoveAllJoinsAsync(int vehicleModelId, CancellationToken ct);
        Task AddJoinsAsync(int vehicleModelId, IEnumerable<int> connectorTypeIds, CancellationToken ct);

        Task SaveAsync(CancellationToken ct);
    }
}
