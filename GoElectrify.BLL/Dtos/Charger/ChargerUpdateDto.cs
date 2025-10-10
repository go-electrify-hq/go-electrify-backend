using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Charger
{
    public sealed class ChargerUpdateDto
    {
        public int? ConnectorTypeId { get; set; }
        public string? Code { get; set; }
        public int? PowerKw { get; set; }
        public string? Status { get; set; }
        public decimal? PricePerKwh { get; set; }
        public string? DockSecretHash { get; set; }
    }
}
