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

        public int? ChargerId { get; set; }               // có thể đặt theo trạm, chưa chốt trụ
        public Charger? Charger { get; set; }

        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }

        public string Status { get; set; } = "PENDING";   // PENDING | CONFIRMED | CANCELED | EXPIRED
        public string Code { get; set; } = default!;      // mã đặt chỗ đưa vào QR/app
        public decimal? EstimatedCost { get; set; }       // tuỳ mô hình, decimal(18,2)

        public ChargingSession? ChargingSession { get; set; }
    }
}
