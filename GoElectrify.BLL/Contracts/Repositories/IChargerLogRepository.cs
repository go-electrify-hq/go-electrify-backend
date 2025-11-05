using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoElectrify.BLL.Entities;

namespace GoElectrify.DAL.Repositories
{
    public interface IChargerLogRepository
    {
        Task<List<ChargerLog>> GetLastByChargerBetweenAsync(int chargerId, DateTime fromUtc, DateTime toUtc, int last, CancellationToken ct);
    }
}
