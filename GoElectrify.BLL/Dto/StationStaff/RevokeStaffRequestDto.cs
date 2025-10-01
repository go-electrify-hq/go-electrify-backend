using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.StationStaff
{
    public class RevokeStaffRequestDto
    {
        [Required, MinLength(3)]
        public string Reason { get; set; } = string.Empty;
    }
}
