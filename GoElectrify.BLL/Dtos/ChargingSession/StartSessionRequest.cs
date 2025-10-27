using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.ChargingSession
{
    public sealed class StartSessionRequest
    {
        public int TargetSoc { get; set; } = 80;
        public required string BookingCode { get; set; }
    }
}
