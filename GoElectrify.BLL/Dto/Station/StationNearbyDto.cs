using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Station
{
    public class StationNearbyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Address { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public double DistanceKm { get; set; }   // khoảng cách đã tính
        public string Status { get; set; } = "ACTIVE";
    }
}
