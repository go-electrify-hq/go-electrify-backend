using Microsoft.AspNetCore.Authorization;

namespace GoElectrify.Api.Auth
{
    public sealed class NoUnpaidSessionsRequirement : IAuthorizationRequirement { }
}
