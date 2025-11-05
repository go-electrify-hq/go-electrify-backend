using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.DAL.Repositories
{
    public interface IVehicleModelConnectorTypeRepository
    {
        Task<bool> IsCompatibleAsync(int vehicleModelId, int connectorTypeId, CancellationToken ct);
    }
}
