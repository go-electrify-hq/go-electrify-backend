using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.VehicleModels
{
    public class CreateVehicleModelDto
    {
        public string ModelName { get; set; } = default!;
        public int MaxPowerKw { get; set; }
        public decimal BatteryCapacityKwh { get; set; } // decimal(12,4)
        public List<int>? ConnectorTypeIds { get; set; } 
    }
}
