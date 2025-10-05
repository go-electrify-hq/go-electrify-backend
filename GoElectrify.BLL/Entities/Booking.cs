using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Entities
{
    public class Booking : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = default!;
        public int StationId { get; set; }
        public Station Station { get; set; } = default!;
        public int ConnectorTypeId { get; set; }
        public ConnectorType ConnectorType { get; set; } = default!;

        public int VehicleModelId { get; set; }
        public VehicleModel VehicleModel { get; set; } = default!;
        public DateTime ScheduledStart { get; set; }
        public int InitialSoc { get; set; }

        public string Status { get; set; } = "PENDING";   // PENDING | CONFIRMED | CANCELED | EXPIRED
        public string Code { get; set; } = default!;      // mã đặt chỗ đưa vào QR/app
        public decimal? EstimatedCost { get; set; }       // giá tiền dự kiến

        public ChargingSession? ChargingSession { get; set; }
    }
}
