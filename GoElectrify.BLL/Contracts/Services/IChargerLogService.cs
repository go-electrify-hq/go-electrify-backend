using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoElectrify.BLL.Dtos.ChargerLogs;
using GoElectrify.BLL.Dtos.ChargingSession;

namespace GoElectrify.BLL.Contracts.Services
{
    public interface IChargerLogService
    {
        Task<PagedResult<ChargerLogItemDto>> GetLogsAsync(
            int chargerId, ChargerLogQueryDto q, CancellationToken ct);
    }
}
