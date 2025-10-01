using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Charger
{
    public sealed class ChargerCreateDto
    {
        public int StationId { get; set; }
        public int ConnectorTypeId { get; set; }
        public string Code { get; set; } = default!;
        public int PowerKw { get; set; }
        public string Status { get; set; } = "ONLINE"; // ONLINE | OFFLINE | MAINTENANCE
        public decimal? PricePerKwh { get; set; }
    }
}
