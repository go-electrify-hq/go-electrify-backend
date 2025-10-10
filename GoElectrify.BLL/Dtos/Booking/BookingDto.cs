using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Booking
{
    public sealed class BookingDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = default!;
        public string Status { get; set; } = default!;
        public DateTime ScheduledStart { get; set; }
        public int InitialSoc { get; set; }
        public int StationId { get; set; }
        public int ConnectorTypeId { get; set; }
        public int VehicleModelId { get; set; }
        public decimal? EstimatedCost { get; set; }
    }
}
