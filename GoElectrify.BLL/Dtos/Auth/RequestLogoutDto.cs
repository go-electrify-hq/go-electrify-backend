using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoElectrify.BLL.Dto.Auth
{
    public sealed record RequestLogoutDto([property: Required] string RefreshToken);
}
