namespace GoElectrify.DockConsole.Contracts
{
    public record PartialLog
    {
        public string? SampleAt { get; init; }
        public decimal? Voltage { get; init; }
        public decimal? Current { get; init; }
        public decimal? PowerKw { get; init; }
        public decimal? SessionEnergyKwh { get; init; }
        public int? SocPercent { get; init; }
        public string? State { get; init; }
        public string? ErrorCode { get; init; }
    }
}
