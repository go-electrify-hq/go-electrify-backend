using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Entities
{
    public class VehicleModelConnectorType
    {
        public int VehicleModelId { get; set; }
        public VehicleModel VehicleModel { get; set; } = default!;

        public int ConnectorTypeId { get; set; }
        public ConnectorType ConnectorType { get; set; } = default!;
    }
}
