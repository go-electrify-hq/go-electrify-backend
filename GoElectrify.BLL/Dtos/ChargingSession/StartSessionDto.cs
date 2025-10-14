using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.ChargingSession
{
    public sealed class StartSessionDto
    {
        public int? ChargerId { get; set; }
        public int VehicleModelId { get; set; }
        public int InitialSoc { get; set; }      // từ 0 đến 100
        public int? TargetSoc { get; set; }       // từ 0 đến 100, null = mặc định 100
        public string? BookingCode { get; set; }
    }
}
