using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Entities
{
    public class StationStaff : BaseEntity
    {
        public int StationId { get; set; }
        public Station Station { get; set; } = default!;

        public int UserId { get; set; }                   // nhân viên (User.Role = Staff)
        public User User { get; set; } = default!;
        public string? RevokedReason { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }
    }
}
