using GoElectrify.BLL.Dto.Charger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.Station
{
    public class StationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Address { get; set; } = default!;
        public string Status { get; set; } = default!;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public List<ChargerDto> Chargers { get; set; } = new();

    }
}
