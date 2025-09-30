using GoElectrify.BLL.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Contracts.Repositories
{
    public interface IConnectorTypeRepository
    {
        Task<List<ConnectorType>> ListAsync(string? search, CancellationToken ct);
        Task<ConnectorType?> GetByIdAsync(int id, CancellationToken ct);
        Task<bool> ExistsByNameAsync(string name, int? excludeId, CancellationToken ct);

        Task AddAsync(ConnectorType entity, CancellationToken ct);
        void Remove(ConnectorType entity);

        // Join helpers để xóa sạch quan hệ trước khi xóa ConnectorType
        Task<bool> HasAnyJoinAsync(int connectorTypeId, CancellationToken ct);
        Task RemoveAllJoinsAsync(int connectorTypeId, CancellationToken ct);

        Task SaveAsync(CancellationToken ct);
    }
}
