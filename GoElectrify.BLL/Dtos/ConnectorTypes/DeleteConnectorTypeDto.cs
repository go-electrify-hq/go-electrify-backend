using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.ConnectorTypes
{
    public class DeleteConnectorTypeDto
    {
        public List<int> Ids { get; set; } = new();
    }
}
