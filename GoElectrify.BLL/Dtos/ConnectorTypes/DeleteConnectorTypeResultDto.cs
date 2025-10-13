using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.ConnectorTypes
{
    public class DeleteConnectorTypeResultDto
    {
        public int Deleted { get; set; }
        public List<int> DeletedIds { get; set; } = new();
        public List<int> BlockedIds { get; set; } = new();
        public List<int> NotFoundIds { get; set; } = new();
    }
}
