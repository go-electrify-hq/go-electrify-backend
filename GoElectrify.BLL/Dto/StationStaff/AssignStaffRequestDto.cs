using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.StationStaff
{
    public class AssignStaffRequestDto
    {
        [Required] public int UserId { get; set; }
        public string? Role { get; set; }

    }
}
