namespace GoElectrify.BLL.Dto.VehicleModels
{
    public class VehicleModelDto
    {
        public int Id { get; set; }
        public string ModelName { get; set; } = default!;
        public int MaxPowerKw { get; set; }
        public decimal BatteryCapacityKwh { get; set; } // decimal(12,4)
        public List<int> ConnectorTypeIds { get; set; } = new List<int>();
    }
}
