using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Incidents
{
    public class AdminIncidentListItemDto
    {
        public int Id { get; set; }
        public int StationId { get; set; }
        public string StationName { get; set; } = string.Empty;
        public int? ChargerId { get; set; }
        public int ReporterUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ReportedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
