using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Station
{
    public class StationCreateDto
    {
        [Required] public string Name { get; set; }
        public string? Description { get; set; }
        [Required] public string Address { get; set; }
        [Range(-90, 90)] public decimal Latitude { get; set; }
        [Range(-180, 180)] public decimal Longitude { get; set; }
    }
}
