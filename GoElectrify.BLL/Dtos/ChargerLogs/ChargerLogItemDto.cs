using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.ChargerLogs
{
    public sealed record ChargerLogItemDto(
        int Id,
        DateTime SampleAt,
        decimal? Voltage,
        decimal? Current,
        decimal? PowerKw,
        decimal? SessionEnergyKwh,
        int? SocPercent,
        string? State,
        string? ErrorCode
    );
}
