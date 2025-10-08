using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Booking
{
    public sealed class CreateBookingDto
    {
        public int StationId { get; set; }
        public int VehicleModelId { get; set; }
        public int ConnectorTypeId { get; set; }
        public DateTime ScheduledStart { get; set; }   // UTC
        public int InitialSoc { get; set; }            // 0..100
    }
}
