using GoElectrify.Api.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoElectrify.Api.Controllers
{

    [ApiController]
    [Route("api/v1/ably")]
    public sealed class AblyController(IAblyService ably) : ControllerBase
    {
        [Authorize]
        [HttpGet("token")]
        public async Task<IActionResult> GetToken([FromQuery] string channel, [FromQuery] string role = "driver", CancellationToken ct = default)
        {
            var cap = role == "driver"
                ? $@"{{""{channel}"":[""subscribe"",""publish""]}}"
                : $@"{{""{channel}"":[""subscribe""]}}";
            var ttl = TimeSpan.FromHours(1);
            var token = await ably.CreateTokenAsync(channel, $"user-{User.Identity?.Name ?? "driver"}", cap, ttl, ct);
            return Ok(new { ok = true, data = new { token, expiresAt = DateTime.UtcNow.Add(ttl) } });
        }
    }
}
