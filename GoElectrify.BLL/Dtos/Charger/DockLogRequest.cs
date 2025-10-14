namespace GoElectrify.BLL.Dto.Charger
{
    public class DockLogRequest
    {
        public int DockId { get; set; }                  // = ChargerId
        public string SecretKey { get; set; } = default!;
        public DateTimeOffset SampleAt { get; set; }     // UTC
        public decimal? Voltage { get; set; }            // V
        public decimal? Current { get; set; }            // A
        public decimal? PowerKw { get; set; }            // kW
        public decimal? SessionEnergyKwh { get; set; }   // kWh cộng dồn trong phiên (nếu dock đo được)
        public int? SocPercent { get; set; }             // %
        public string? State { get; set; }               // IDLE | CHARGING | FAULT...
        public string? ErrorCode { get; set; }
        public Dictionary<string, object>? Extra { get; set; } // nếu cần mở rộng, có thể bỏ qua
    }
}
