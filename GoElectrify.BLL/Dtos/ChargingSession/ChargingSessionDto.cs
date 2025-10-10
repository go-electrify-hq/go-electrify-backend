using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.ChargingSession
{
    public sealed class ChargingSessionDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = default!;
        public DateTime StartedAt { get; set; }
        public int ChargerId { get; set; }
        public int StationId { get; set; }          // từ Charger
        public int ConnectorTypeId { get; set; }    // từ Charger
        public int? BookingId { get; set; }
    }
}
