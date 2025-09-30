using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.ConnectorTypes
{
    public class CreateConnectorTypeDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int MaxPowerKw { get; set; }
    }
}
