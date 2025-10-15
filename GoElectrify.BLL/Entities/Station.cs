namespace GoElectrify.BLL.Entities
{
    public class Station : BaseEntity
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public string Address { get; set; } = default!;

        public string? ImageUrl { get; set; } = default!;
        public decimal Latitude { get; set; }   // decimal(10,6)
        public decimal Longitude { get; set; }  // decimal(10,6)

        // Gợi ý: ACTIVE | INACTIVE | MAINTENANCE
        public string Status { get; set; } = "ACTIVE";

        public ICollection<Charger> Chargers { get; set; } = new List<Charger>();
        public ICollection<StationStaff> StationStaff { get; set; } = new List<StationStaff>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
    }
}
