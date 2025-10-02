using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Incidents
{
    public class IncidentCreateDto
    {
        public int? ChargerId { get; set; }         // optional: sự cố ở trụ cụ thể
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public string Severity { get; set; } = "LOW";
        public DateTime? ReportedAt { get; set; }   // nếu null server sẽ set UtcNow
    }
}
