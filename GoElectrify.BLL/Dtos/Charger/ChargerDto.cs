namespace GoElectrify.BLL.Dto.Charger
{
    public sealed class ChargerDto
    {

        public int Id { get; set; }
        public int StationId { get; set; }
        public int ConnectorTypeId { get; set; }
        public string Code { get; set; } = default!;
        public int PowerKw { get; set; }
        public string Status { get; set; } = default!;
        public decimal? PricePerKwh { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
