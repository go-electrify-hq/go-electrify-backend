using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.StationStaff
{
    public class StationStaffDto
    {
        public int StationId { get; set; }
        public int UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? UserFullName { get; set; }
        public string Role { get; set; } = "STAFF";
        public DateTime AssignedAt { get; set; }
    }
}
