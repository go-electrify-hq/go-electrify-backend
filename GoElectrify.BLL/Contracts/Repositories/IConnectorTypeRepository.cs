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

        Task<HashSet<int>> GetExistingIdsAsync(IEnumerable<int> ids, CancellationToken ct);   // NEW (trả HashSet nhanh)
        Task<HashSet<int>> FindBlockedIdsAsync(IEnumerable<int> ids, CancellationToken ct);   // chỉ Chargers + Bookings

        Task RemoveAllJoinsAsync(int connectorTypeId, CancellationToken ct);                 // xoá join cho 1 id
        Task RemoveAllJoinsAsync(IEnumerable<int> ids, CancellationToken ct);                // xoá join cho nhiều id

        Task<int> BulkDeleteAsync(IEnumerable<int> ids, CancellationToken ct);               // set-based delete
        Task<int> DeleteManySafeAsync(IEnumerable<int> ids, CancellationToken ct);           // NEW: dọn join + xoá trong 1 TXN

        Task SaveAsync(CancellationToken ct);
    }
}
