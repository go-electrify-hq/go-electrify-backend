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

        public int DurationSeconds { get; set; }

        public int SocStart { get; set; }            // % lúc bắt đầu
        public int? SocEnd { get; set; }            // % lúc kết thúc, tuỳ chọn
        public int? ParkingMinutes { get; set; }
        public string? AblyChannel { get; set; }   // ví dụ: "ge:session:{Id}"
        public string? JoinCode { get; set; }      // ví dụ: "8M3Q9K", TTL ngắn
        public int? TargetSoc { get; set; }        // % mong muốn (Dashboard gửi)
        public int? FinalSoc { get; set; }         // % thực tế khi complete

        // Chuẩn hoá cho handshake -> PENDING, khi start -> RUNNING
        // (các flow cũ vẫn set RUNNING như trước, không sao)
        public string Status { get; set; } = "PENDING";   // RUNNING | STOPPED | COMPLETED | FAILED

        public decimal EnergyKwh { get; set; }            // tổng kWh đã sạc, decimal(12,4)
        public decimal? AvgPowerKw { get; set; }          // tuỳ chọn
        public decimal? Cost { get; set; }                // decimal(18,2)

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
