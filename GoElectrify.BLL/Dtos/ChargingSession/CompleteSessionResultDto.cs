using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.ChargingSession
{
    public sealed record CompleteSessionResult(
        int Id,
        string Status,
        decimal EnergyKwh,
        decimal? Cost,
        DateTime EndedAt
    );
}
