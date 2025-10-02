using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Incidents
{
    public class IncidentListQueryDto
    {
        public string? Status { get; set; }           // OPEN | IN_PROGRESS | RESOLVED | CLOSED
        public string? Severity { get; set; }         // LOW | MEDIUM | HIGH | CRITICAL
        public DateTime? FromReportedAt { get; set; }
        public DateTime? ToReportedAt { get; set; }
    }
}
