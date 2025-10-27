using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.ChargingSession
{
    public sealed class StopSessionDto
    {
        public string? Reason { get; set; }     // "target_soc" | "user_request" | "timeout" | "error" ...
        public int? FinalSoc { get; set; }
        public decimal? EnergyKwh { get; set; }
    }
}
