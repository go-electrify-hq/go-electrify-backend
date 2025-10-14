namespace GoElectrify.DockConsole.Contracts
{
    public record StartSessionInput
    {
        public int? BookingId { get; init; }
        public string? BookingCode { get; init; }
        public int? ChargerId { get; init; }     // optional khi auto-assign theo booking
        public int? VehicleModelId { get; init; }
        public int InitialSoc { get; init; }    // 0..100
    }
}
