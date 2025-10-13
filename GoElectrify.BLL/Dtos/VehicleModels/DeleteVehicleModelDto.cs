using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.VehicleModels
{
    public class DeleteVehicleModelDto
    {
        public List<int> Ids { get; set; } = new();
    }
}
