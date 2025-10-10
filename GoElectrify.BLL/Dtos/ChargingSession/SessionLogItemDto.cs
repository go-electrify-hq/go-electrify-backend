using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.ChargingSession
{
    public sealed class SessionLogItemDto
    {
        public DateTime At { get; init; }                 // UTC
        public decimal? Voltage { get; init; }            // V
        public decimal? Current { get; init; }            // A
        public decimal? PowerKw { get; init; }            // kW
        public decimal? SessionEnergyKwh { get; init; }   // kWh (cộng dồn trong phiên nếu có)
        public int? SocPercent { get; init; }             // %
        public string? State { get; init; }               // IDLE | CHARGING | ...
        public string? ErrorCode { get; init; }
    }
}
