using System.ComponentModel.DataAnnotations;

namespace GoElectrify.BLL.Dto.StationStaff
{
    public class RevokeStaffRequestDto
    {
        [Required, MinLength(3)]
        public string Reason { get; set; } = string.Empty;
    }
}
