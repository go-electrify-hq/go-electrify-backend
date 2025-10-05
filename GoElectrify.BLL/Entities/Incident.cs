using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Entities
{
    public class Incident : BaseEntity
    {
        public int StationId { get; set; }
        public Station Station { get; set; } = default!;

        public int? ChargerId { get; set; }               // optional: sự cố ở cấp trạm hoặc trụ
        public Charger? Charger { get; set; }

        public int ReportedByStationStaffId { get; set; }         // thường là Staff
        public StationStaff ReportedBy { get; set; } = default!;

        public string Title { get; set; } = default!;
        public string? Description { get; set; }

        public string Priority { get; set; } = "LOW";     // LOW | MEDIUM | HIGH | CRITICAL
        public string Status { get; set; } = "OPEN";      // OPEN | IN_PROGRESS | RESOLVED | CLOSED
        public string? Response { get; set; }

        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}
