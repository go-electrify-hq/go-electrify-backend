namespace GoElectrify.BLL.Entities
{
    public class VehicleModel : BaseEntity
    {
        public string ModelName { get; set; } = default!;
        public int MaxPowerKw { get; set; }
        public decimal BatteryCapacityKwh { get; set; } // decimal(12,4)

        public ICollection<VehicleModelConnectorType> VehicleModelConnectorTypes { get; set; } = new List<VehicleModelConnectorType>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
