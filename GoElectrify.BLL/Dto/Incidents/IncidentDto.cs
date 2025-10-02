using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Incidents
{
    public class IncidentDto
    {
        public int Id { get; set; }
        public int StationId { get; set; }
        public int? ChargerId { get; set; }
        public int ReportedByUserId { get; set; }

        public string Title { get; set; } = default!;
        public string? Description { get; set; }

        public string Severity { get; set; } = default!;
        public string Status { get; set; } = default!;

        public DateTime ReportedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
