using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dtos.Users
{
    public sealed class UpdateUserRoleRequest
    {
        public string Role { get; set; } = default!;
        public bool ForceSignOut { get; set; } = true;
    }

    public sealed class UserRoleChangedDto
    {
        public int UserId { get; set; }
        public string OldRole { get; set; } = default!;
        public string NewRole { get; set; } = default!;
    }
}
