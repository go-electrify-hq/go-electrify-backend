namespace GoElectrify.BLL.Entities
{
    public class Charger : BaseEntity
    {
        public int StationId { get; set; }
        public Station Station { get; set; } = default!;

        public int ConnectorTypeId { get; set; }
        public ConnectorType ConnectorType { get; set; } = default!;
        public string Code { get; set; } = default!;      // mã trụ/QR
        public int PowerKw { get; set; }
        public string Status { get; set; } = "ONLINE";     // ONLINE | OFFLINE | MAINTENANCE

        // Giá—tuỳ mô hình: có thể để ở Station, hoặc override theo Charger
        public decimal? PricePerKwh { get; set; }         // decimal(18,4)
        public string? DockSecretHash { get; set; }     // "{saltHex}.{hashHex}" - KHÔNG lưu plain text
        public string? AblyChannel { get; set; }        // ví dụ "ge:dock:{Id}" (có thể để null và compute)
        public string? DockStatus { get; set; } = "DISCONNECTED"; // CONNECTED | DISCONNECTED
        public DateTime? LastConnectedAt { get; set; }
        public DateTime? LastPingAt { get; set; }

        public ICollection<ChargingSession> ChargingSessions { get; set; } = new List<ChargingSession>();
        public ICollection<ChargerLog> ChargerLogs { get; set; } = new List<ChargerLog>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
