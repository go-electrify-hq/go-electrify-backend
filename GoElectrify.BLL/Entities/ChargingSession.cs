using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace GoElectrify.BLL.Entities
{
    public class ChargingSession : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = default!;

        public int StationId { get; set; }
        public Station Station { get; set; } = default!;

        public int ChargerId { get; set; }
        public Charger Charger { get; set; } = default!;

        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        public string Status { get; set; } = "RUNNING";   // RUNNING | STOPPED | COMPLETED | FAILED

        public decimal EnergyKwh { get; set; }            // tổng kWh đã sạc, decimal(12,4)
        public decimal? AvgPowerKw { get; set; }          // tuỳ chọn
        public decimal? Cost { get; set; }                // decimal(18,2)

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
