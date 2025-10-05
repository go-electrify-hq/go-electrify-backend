using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Entities
{
    public class ConnectorType : BaseEntity
    {
        public string Name { get; set; } = default!;       // CCS | CHAdeMO | AC
        public string? Description { get; set; }
        public int MaxPowerKw { get; set; }

        public ICollection<Charger> Chargers { get; set; } = new List<Charger>();
        public ICollection<VehicleModelConnectorType> VehicleModelConnectorTypes { get; set; } = new List<VehicleModelConnectorType>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}
