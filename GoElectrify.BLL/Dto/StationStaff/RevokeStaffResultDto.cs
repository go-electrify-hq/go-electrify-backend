using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.StationStaff
{
    public class RevokeStaffResultDto
    {
        public int StationId { get; set; }
        public int UserId { get; set; }

        /// <summary>"revoked" | "noop_already_revoked"</summary>
        public string Action { get; set; } = "revoked";

        public DateTime? RevokedAt { get; set; }
        public string? RevokedReason { get; set; }
    }
}
