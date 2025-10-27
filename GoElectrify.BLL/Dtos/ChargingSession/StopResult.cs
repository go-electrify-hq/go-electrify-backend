using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.ChargingSession
{
    public sealed record StopResult(
        DateTime EndedAt,
        int DurationMinutes,
        decimal EnergyKwh,
        decimal? AvgPowerKw,
        decimal? Cost
    );
}
