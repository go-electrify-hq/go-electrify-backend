using GoElectrify.BLL.Dto.ConnectorTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IConnectorTypeService
    {
        Task<List<ConnectorTypeDto>> ListAsync(string? search, CancellationToken ct);
        Task<ConnectorTypeDto?> GetAsync(int id, CancellationToken ct);
        Task<int> CreateAsync(CreateConnectorTypeDto dto, CancellationToken ct);
        Task UpdateAsync(int id, UpdateConnectorTypeDto dto, CancellationToken ct);
        Task DeleteAsync(int id, CancellationToken ct);
    }
}
