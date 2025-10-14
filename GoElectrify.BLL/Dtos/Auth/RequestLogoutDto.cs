using System.ComponentModel.DataAnnotations;

namespace GoElectrify.BLL.Dto.Auth
{
    public sealed record RequestLogoutDto([property: Required] string RefreshToken);
}
