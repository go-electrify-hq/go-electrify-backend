using System.ComponentModel.DataAnnotations;

namespace GoElectrify.BLL.Dto.StationStaff
{
    public class AssignStaffRequestDto
    {
        [Required] public int UserId { get; set; }


    }
}
