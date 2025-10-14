namespace GoElectrify.BLL.Entities
{
    public class ChargingSession : BaseEntity
    {
        public int? BookingId { get; set; }
        public Booking? Booking { get; set; }
        public int ChargerId { get; set; }
        public Charger Charger { get; set; } = default!;

        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }

        public int DurationMinutes { get; set; }

        public int SocStart { get; set; }            // % lúc bắt đầu
        public int? SocEnd { get; set; }            // % lúc kết thúc, tuỳ chọn
        public int? ParkingMinutes { get; set; }

        public string Status { get; set; } = "RUNNING";   // RUNNING | STOPPED | COMPLETED | FAILED

        public decimal EnergyKwh { get; set; }            // tổng kWh đã sạc, decimal(12,4)
        public decimal? AvgPowerKw { get; set; }          // tuỳ chọn
        public decimal? Cost { get; set; }                // decimal(18,2)

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
