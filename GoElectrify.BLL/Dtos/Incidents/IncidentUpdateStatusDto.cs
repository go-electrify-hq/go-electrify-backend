using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Incidents
{
    public class IncidentUpdateStatusDto
    {
        public string Status { get; set; } = default!;    // IN_PROGRESS | RESOLVED | CLOSED
        public DateTime? ResolvedAt { get; set; }
    }
}
