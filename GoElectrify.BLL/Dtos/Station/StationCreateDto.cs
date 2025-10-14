using System.ComponentModel.DataAnnotations;

namespace GoElectrify.BLL.Dto.Station
{
    public class StationCreateDto
    {
        [Required] public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required] public string Address { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        [Range(-90, 90)] public decimal Latitude { get; set; }
        [Range(-180, 180)] public decimal Longitude { get; set; }
    }
}
