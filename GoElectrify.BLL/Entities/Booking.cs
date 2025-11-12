namespace GoElectrify.BLL.Entities
{
    public class Booking : BaseEntity
    {
        public int UserId { get; set; }
        public User User { get; set; } = default!;
        public int StationId { get; set; }
        public Station Station { get; set; } = default!;
        public int? ChargerId { get; set; }
        public Charger? Charger { get; set; }
        public int ConnectorTypeId { get; set; }
        public ConnectorType ConnectorType { get; set; } = default!;

        public int? VehicleModelId { get; set; }
        public VehicleModel? VehicleModel { get; set; } = default!;
       
        public DateTime ScheduledStart { get; set; }
        public int InitialSoc { get; set; }

        public string Status { get; set; } = "PENDING";   
        public string Code { get; set; } = default!;      
        public decimal? EstimatedCost { get; set; }      

        public ChargingSession? ChargingSession { get; set; }
    }
}
